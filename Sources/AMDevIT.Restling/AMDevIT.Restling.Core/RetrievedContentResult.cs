using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core
{
    public class RetrievedContentResult(object? content, bool isBinaryData, MediaTypeHeaderValue? contentType)
    {
        #region Properties

        public object? Content { get; } = content;
        public bool IsBinaryData { get; } = isBinaryData;
        public MediaTypeHeaderValue? ContentType { get; } = contentType;

        #endregion

        #region Methods

        public override string ToString()
        {
            string contentOutput;
            contentOutput = this.ToStringContent();
            return $"[Content:{contentOutput},ContentType:{this.ContentType}, IsBinaryData: {this.IsBinaryData}]";
        }

        public string ToStringContent()
        {
            string contentOutput;

            if (this.IsBinaryData && this.Content is IEnumerable<byte> contentBytesEnumerable)
            {
                byte[] contentBytes = contentBytesEnumerable.ToArray();
                contentOutput = Convert.ToBase64String(contentBytes);
            }
            else
                contentOutput = this.Content?.ToString() ?? string.Empty;
            return contentOutput;
        }

        #endregion

    }
}
