using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Represents a parsed MIME multipart entity.</summary>
    public sealed class MultipartDocument
    {
        #region Properties

        public MediaTypeHeaderValue ContentType { get; }
        public IReadOnlyList<MultipartPart> Parts { get; }
        public byte[] Preamble { get; }
        public byte[] Epilogue { get; }

        /// <summary>Gets the root part selected by multipart/related's start parameter, or the first part.</summary>
        public MultipartPart? RootPart
        {
            get
            {
                NameValueHeaderValue? start;
                string? contentId;

                if (!string.Equals(this.ContentType.MediaType, "multipart/related", StringComparison.OrdinalIgnoreCase))
                    return this.Parts.FirstOrDefault();

                start = this.ContentType.Parameters.FirstOrDefault(parameter =>
                    string.Equals(parameter.Name, "start", StringComparison.OrdinalIgnoreCase));
                contentId = start?.Value?.Trim().Trim('"').Trim('<', '>');
                return string.IsNullOrWhiteSpace(contentId)
                    ? this.Parts.FirstOrDefault()
                    : this.Parts.FirstOrDefault(part => string.Equals(part.ContentId, contentId, StringComparison.Ordinal));
            }
        }

        #endregion

        #region .ctor

        internal MultipartDocument(MediaTypeHeaderValue contentType,
                                   IEnumerable<MultipartPart> parts,
                                   byte[] preamble,
                                   byte[] epilogue)
        {
            this.ContentType = contentType;
            this.Parts = new ReadOnlyCollection<MultipartPart>(parts.ToArray());
            this.Preamble = preamble;
            this.Epilogue = epilogue;
        }

        #endregion
    }
}
