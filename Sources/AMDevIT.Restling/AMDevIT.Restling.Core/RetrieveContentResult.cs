using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core
{
    internal class RetrieveContentResult(object? content, bool isBinaryData, MediaTypeHeaderValue? contentType)
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

            if (this.IsBinaryData && this.Content is IEnumerable<byte> contentBytesEnumerable)
            {
                byte[] contentBytes = contentBytesEnumerable.ToArray();
                contentOutput = Convert.ToBase64String(contentBytes);
            }
            else            
                contentOutput = this.Content?.ToString() ?? string.Empty;
            
            return $"[Content:{contentOutput},ContentType:{this.ContentType}, IsBinaryData: {this.IsBinaryData}]";
        }

        #endregion

    }
}
