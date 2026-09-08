using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Buffered binary fallback. Register after specific codecs.</summary>
    public sealed class BinaryContentCodec : IContentCodec
    {
        #region Properties

        public bool IsBinary => true;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType) => true;

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => true;

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            if (typeof(T) == typeof(byte[]))
                return (T)(object)content;
            if (typeof(T) == typeof(string))
                return (T)(object)Convert.ToBase64String(content);
            return default;
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            if (value is not byte[] bytes)
                throw new ArgumentException("The binary codec requires a byte array.", nameof(value));
            ByteArrayContent content = new(bytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType.ToString());
            return content;
        }

        #endregion
    }
}
