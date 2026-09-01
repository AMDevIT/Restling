using AMDevIT.Restling.Core.Codecs;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Represents one MIME multipart body part without interpreting file names as paths.</summary>
    public sealed class MultipartPart
    {
        #region Fields

        private readonly ContentCodecRegistry codecs;
        private readonly ContentCodecContext codecContext;

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
        public byte[] RawContent { get; }
        public MediaTypeHeaderValue? ContentType { get; }
        public ContentDispositionHeaderValue? ContentDisposition { get; }
        public string? Name => TrimQuotes(this.ContentDisposition?.Name);
        public string? FileName => TrimQuotes(this.ContentDisposition?.FileNameStar ?? this.ContentDisposition?.FileName);
        public string? ContentId => this.Headers.TryGetValue("Content-ID", out IReadOnlyList<string>? values) && values.Count > 0
            ? values[0].Trim().Trim('<', '>')
            : null;
        public ContentRangeHeaderValue? ContentRange { get; }
        public MultipartDocument? NestedContent { get; }

        #endregion

        #region .ctor

        internal MultipartPart(IDictionary<string, IReadOnlyList<string>> headers,
                               byte[] rawContent,
                               MultipartDocument? nestedContent,
                               ContentCodecRegistry codecs,
                               ContentCodecContext codecContext)
        {
            Dictionary<string, IReadOnlyList<string>> snapshot = new(headers, StringComparer.OrdinalIgnoreCase);
            this.Headers = new ReadOnlyDictionary<string, IReadOnlyList<string>>(snapshot);
            this.RawContent = rawContent;
            this.NestedContent = nestedContent;
            this.codecs = codecs;
            this.codecContext = codecContext;

            if (headers.TryGetValue("Content-Type", out IReadOnlyList<string>? contentTypes) && contentTypes.Count > 0)
                this.ContentType = MediaTypeHeaderValue.Parse(contentTypes[0]);
            if (headers.TryGetValue("Content-Disposition", out IReadOnlyList<string>? dispositions) && dispositions.Count > 0)
                this.ContentDisposition = ContentDispositionHeaderValue.Parse(dispositions[0]);
            if (headers.TryGetValue("Content-Range", out IReadOnlyList<string>? ranges) && ranges.Count > 0)
                this.ContentRange = ContentRangeHeaderValue.Parse(ranges[0]);
        }

        #endregion

        #region Methods

        /// <summary>Deserializes this part with the codec registry that parsed the response.</summary>
        public T? Deserialize<T>()
        {
            if (this.NestedContent != null && typeof(T) == typeof(MultipartDocument))
                return (T)(object)this.NestedContent;
            if (typeof(T) == typeof(byte[]))
                return (T)(object)this.RawContent;

            IContentCodec? codec = this.codecs.FindReader(this.ContentType?.MediaType);
            if (codec == null)
                return default;
            return codec.Deserialize<T>(this.RawContent, this.ContentType, this.codecContext);
        }

        private static string? TrimQuotes(string? value)
        {
            return value?.Trim().Trim('"');
        }

        #endregion
    }
}
