using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Network;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Represents a reusable multipart request whose contents are created for each execution.</summary>
    public sealed class MultipartRequest : RestRequest
    {
        #region Fields

        private readonly List<PartFactory> parts = [];
        private readonly Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Properties

        public string Subtype { get; }
        public string? Boundary { get; init; }
        public int PartCount => this.parts.Count;
        public IReadOnlyDictionary<string, string> Parameters => this.parameters;

        #endregion

        #region .ctor

        /// <summary>Creates a multipart/form-data request.</summary>
        public MultipartRequest(string uri, HttpMethod method)
            : this(uri, method, "form-data", null)
        {
        }

        /// <summary>Creates a multipart request with an explicit subtype.</summary>
        public MultipartRequest(string uri, HttpMethod method, string subtype, string? customMethod = null)
            : base(uri, method, customMethod)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subtype);
            if (subtype.Contains('/') || subtype.Any(character => char.IsWhiteSpace(character)))
                throw new ArgumentException("The multipart subtype is invalid.", nameof(subtype));
            this.Subtype = subtype;
        }

        /// <summary>Creates a multipart request with explicit headers and subtype.</summary>
        public MultipartRequest(string uri,
                                HttpMethod method,
                                RequestHeaders headers,
                                string subtype = "form-data",
                                string? customMethod = null)
            : base(uri, method, headers, customMethod)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subtype);
            if (subtype.Contains('/') || subtype.Any(character => char.IsWhiteSpace(character)))
                throw new ArgumentException("The multipart subtype is invalid.", nameof(subtype));
            this.Subtype = subtype;
        }

        #endregion

        #region Methods

        /// <summary>Adds a UTF-8 text part.</summary>
        public MultipartRequest AddText(string name, string value, string contentType = HttpMediaType.TextPlain)
        {
            MediaTypeHeaderValue mediaType;

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);
            mediaType = MediaTypeHeaderValue.Parse(contentType);
            this.parts.Add(new PartFactory((_, context) => context.CreateTextContent(value, mediaType), name, null));
            return this;
        }

        /// <summary>Adds a reusable, buffered binary part.</summary>
        public MultipartRequest AddBytes(string name,
                                         byte[] content,
                                         string? fileName = null,
                                         string contentType = HttpMediaType.ApplicationOctetStream)
        {
            byte[] snapshot;

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(content);
            snapshot = content.ToArray();
            return this.AddFactory(() => new ByteArrayContent(snapshot),
                                   name,
                                   fileName,
                                   MediaTypeHeaderValue.Parse(contentType));
        }

        /// <summary>Adds a stream part created and owned separately for every request execution.</summary>
        public MultipartRequest AddStream(string name,
                                          Func<Stream> streamFactory,
                                          string? fileName = null,
                                          string contentType = HttpMediaType.ApplicationOctetStream)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(streamFactory);
            return this.AddFactory(() => new StreamContent(streamFactory() ??
                                                           throw new InvalidOperationException("The stream factory returned null.")),
                                   name,
                                   fileName,
                                   MediaTypeHeaderValue.Parse(contentType));
        }

        /// <summary>Adds an arbitrary HTTP content created and owned separately for every execution.</summary>
        public MultipartRequest AddContent(Func<HttpContent> contentFactory,
                                           string? name = null,
                                           string? fileName = null)
        {
            ArgumentNullException.ThrowIfNull(contentFactory);
            this.parts.Add(new PartFactory((_, _) => contentFactory() ??
                                                     throw new InvalidOperationException("The content factory returned null."),
                                           name,
                                           fileName));
            return this;
        }

        /// <summary>Adds an object serialized by the registered writer for the supplied media type.</summary>
        public MultipartRequest AddObject<T>(string name,
                                             T value,
                                             string contentType,
                                             string? fileName = null)
        {
            MediaTypeHeaderValue mediaType;

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            mediaType = MediaTypeHeaderValue.Parse(contentType);
            this.parts.Add(new PartFactory((codecs, context) =>
            {
                IContentCodec codec = codecs.FindWriter(mediaType.MediaType) ??
                                      throw new NotSupportedException($"No writer is registered for {mediaType.MediaType}.");
                return codec.Serialize(value, mediaType, context);
            },
                                           name,
                                           fileName));
            return this;
        }

        /// <summary>Adds a quoted top-level multipart media-type parameter.</summary>
        public MultipartRequest AddParameter(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);
            if (string.Equals(name, "boundary", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Set Boundary instead of adding a boundary parameter.", nameof(name));
            this.parameters[name] = value;
            return this;
        }

        internal System.Net.Http.MultipartContent BuildContent(ContentCodecRegistry codecs, ContentCodecContext context)
        {
            System.Net.Http.MultipartContent result;
            string boundary = this.Boundary ?? Guid.NewGuid().ToString("N");

            result = string.Equals(this.Subtype, "form-data", StringComparison.OrdinalIgnoreCase)
                ? new MultipartFormDataContent(boundary)
                : new System.Net.Http.MultipartContent(this.Subtype, boundary);

            foreach (KeyValuePair<string, string> parameter in this.parameters)
            {
                string escaped = parameter.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                result.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue(parameter.Key, $"\"{escaped}\""));
            }

            try
            {
                foreach (PartFactory part in this.parts)
                {
                    HttpContent? content = null;
                    bool added = false;
                    try
                    {
                        if (result is MultipartFormDataContent && string.IsNullOrWhiteSpace(part.Name))
                            throw new InvalidOperationException("multipart/form-data parts require a name.");

                        content = part.Factory(codecs, context);
                        if (part.ContentType != null)
                            content.Headers.ContentType = MediaTypeHeaderValue.Parse(part.ContentType.ToString());

                        if (result is MultipartFormDataContent formData)
                        {
                            if (part.FileName == null)
                                formData.Add(content, part.Name!);
                            else
                                formData.Add(content, part.Name!, part.FileName);
                        }
                        else
                        {
                            if (part.Name != null || part.FileName != null)
                            {
                                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                                {
                                    Name = part.Name,
                                    FileName = part.FileName
                                };
                            }
                            result.Add(content);
                        }
                        added = true;
                    }
                    finally
                    {
                        if (!added)
                            content?.Dispose();
                    }
                }
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private MultipartRequest AddFactory(Func<HttpContent> factory,
                                            string name,
                                            string? fileName,
                                            MediaTypeHeaderValue contentType)
        {
            this.parts.Add(new PartFactory((_, _) => factory(), name, fileName, contentType));
            return this;
        }

        private sealed class PartFactory(Func<ContentCodecRegistry, ContentCodecContext, HttpContent> factory,
                                         string? name,
                                         string? fileName,
                                         MediaTypeHeaderValue? contentType = null)
        {
            #region Properties

            public Func<ContentCodecRegistry, ContentCodecContext, HttpContent> Factory { get; } = factory;
            public string? Name { get; } = name;
            public string? FileName { get; } = fileName;
            public MediaTypeHeaderValue? ContentType { get; } = contentType;

            #endregion
        }

        #endregion
    }
}
