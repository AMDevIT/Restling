using AMDevIT.Restling.Core.Serialization;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>JSON codec retaining automatic Newtonsoft.Json/System.Text.Json selection.</summary>
    public sealed class JsonContentCodec : IContentCodec
    {
        #region Properties

        public bool IsBinary => false;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            return mediaType?.StartsWith("application/", StringComparison.OrdinalIgnoreCase) == true &&
                   mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => this.CanRead(mediaType);

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            JsonSerialization serializer = new(context.Logger);
            return serializer.Deserialize<T>(context.DecodeText(content, contentType), context.JsonSerializerLibrary);
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            JsonSerialization serializer = new(context.Logger);
            string json = value is null ? string.Empty : serializer.Serialize(value, context.JsonSerializerLibrary);
            return context.CreateTextContent(json, contentType);
        }

        #endregion
    }
}
