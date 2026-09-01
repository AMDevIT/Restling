using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Text and primitive conversion for the legacy textual media types.</summary>
    public sealed class TextContentCodec : IContentCodec
    {
        #region Properties

        public bool IsBinary => false;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            return mediaType?.ToLowerInvariant() is "text/plain" or "text/html" or "text/css" or "text/javascript" or "image/svg+xml";
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => this.CanRead(mediaType);

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            string text = context.DecodeText(content, contentType);
            if (typeof(T) == typeof(string))
                return (T)(object)text;
            return typeof(T).IsPrimitive ? (T)Convert.ChangeType(text, typeof(T)) : default;
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            return context.CreateTextContent(value?.ToString() ?? string.Empty, contentType);
        }

        #endregion
    }
}
