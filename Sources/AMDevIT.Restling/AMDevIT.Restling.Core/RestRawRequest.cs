using AMDevIT.Restling.Core.Network;

namespace AMDevIT.Restling.Core
{
    public class RestRawRequest
        : RestRequest
    {
        #region Properties

        public string? Content
        {
            get;
            set;
        }

        public string? AcceptType
        {
            get;
            set;
        }

        public string? ContentType
        {
            get;
            set;
        }

        #endregion

        #region .ctor

        public RestRawRequest(string uri, 
                              HttpMethod method, 
                              string? customMethod = null) 
            : base(uri, method, customMethod)
        {
        }

        public RestRawRequest(string uri, 
                              HttpMethod method, 
                              RequestHeaders headers, 
                              string? customMethod = null) 
            : base(uri, method, headers, customMethod)
        {
        }

        public RestRawRequest(string uri,
                              HttpMethod method,
                              string? content,
                              string? contentType,
                              string? customMethod = null)
            : base(uri, method, customMethod)
        {
            this.Content = content;
            this.ContentType = contentType;
        }

        public RestRawRequest(string uri,
                            HttpMethod method,
                            string? content,
                            string? customMethod = null)
          : this(uri, method, content: content, contentType: null, customMethod)
        {
        }

        public RestRawRequest(string uri,
                              HttpMethod method,
                              string? content,
                              RequestHeaders headers,
                              string? customMethod = null)
            : this(uri, 
                   method, 
                   content: content, 
                   contentType: null, 
                   headers, 
                   customMethod)
        {
        }

        public RestRawRequest(string uri,
                              HttpMethod method,
                              string? content,
                              string? contentType,
                              RequestHeaders headers,
                              string? customMethod = null)
            : base(uri, method, headers, customMethod)
        {
            this.Content = content;
            this.ContentType = contentType;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{base.ToString()} - Content: {this.Content ?? "No content"} " +
                   $"- ContentType: {this.ContentType ?? "No content type"} " +
                   $"- Accept Type: {this.AcceptType ?? "No accept type"}";
        }

        #endregion
    }
}
