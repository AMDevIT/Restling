using AMDevIT.Restling.Core.Codecs;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Tests.Codecs
{
    internal sealed class UpperCaseContentCodec : IContentCodec
    {
        #region Properties

        public bool IsBinary => false;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType) => mediaType == "text/plain";

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => mediaType == "text/plain";

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            return typeof(T) == typeof(string)
                ? (T)(object)context.DecodeText(content, contentType).ToUpperInvariant()
                : default;
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            return context.CreateTextContent(value?.ToString()?.ToUpperInvariant() ?? string.Empty, contentType);
        }

        #endregion
    }
}
