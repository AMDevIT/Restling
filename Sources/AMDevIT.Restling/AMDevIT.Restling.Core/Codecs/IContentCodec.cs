using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Converts buffered HTTP content. Implementations must support concurrent calls.</summary>
    public interface IContentCodec
    {
        #region Properties

        /// <summary>Whether the original response content is binary rather than text.</summary>
        bool IsBinary { get; }

        #endregion

        #region Methods

        /// <summary>Determines whether this codec reads a media type without parameters.</summary>
        bool CanRead(string? mediaType);

        /// <summary>Determines whether this codec writes a media type without parameters.</summary>
        bool CanWrite(string? mediaType);

        /// <summary>Decodes a buffered response. The caller retains ownership of the bytes.</summary>
        T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context);

        /// <summary>Encodes a request. The caller owns and disposes the returned content.</summary>
        HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context);

        #endregion
    }
}
