using AMDevIT.Restling.Core.Multipart;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Decodes buffered MIME multipart responses into structured documents.</summary>
    public sealed class MultipartContentCodec : IContentCodec
    {
        #region Fields

        private readonly MultipartOptions options;

        #endregion

        #region Properties

        public bool IsBinary => true;

        #endregion

        #region .ctor

        /// <summary>Creates a codec with the default multipart safety limits.</summary>
        public MultipartContentCodec()
            : this(new MultipartOptions())
        {
        }

        /// <summary>Creates a codec with explicit multipart safety limits.</summary>
        /// <param name="options">Limits applied to multipart parsing.</param>
        public MultipartContentCodec(MultipartOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();
            this.options = options;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            return mediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => false;

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            ContentCodecRegistry codecs;
            MultipartDocument document;

            ArgumentNullException.ThrowIfNull(contentType);
            codecs = context.Codecs ?? new ContentCodecRegistry();
            document = MultipartParser.Parse(content, contentType, codecs, context, this.options);

            if (typeof(T) == typeof(MultipartDocument))
                return (T)(object)document;
            if (typeof(T) == typeof(IReadOnlyList<MultipartPart>))
                return (T)(object)document.Parts;
            throw new NotSupportedException($"Multipart content cannot be deserialized as {typeof(T).FullName}.");
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            throw new NotSupportedException("Multipart requests are created with MultipartRequest.");
        }

        #endregion
    }
}
