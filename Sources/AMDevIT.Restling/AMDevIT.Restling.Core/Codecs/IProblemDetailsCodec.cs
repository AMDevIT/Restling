using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Exposes structured HTTP problems independently of the success model.</summary>
    public interface IProblemDetailsCodec : IContentCodec
    {
        #region Methods

        /// <summary>Decodes a problem without interpreting its status as the transport status.</summary>
        RestProblemDetails? DeserializeProblem(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context);

        #endregion
    }
}
