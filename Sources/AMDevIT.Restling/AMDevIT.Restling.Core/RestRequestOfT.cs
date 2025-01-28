using AMDevIT.Restling.Core.Network;

namespace AMDevIT.Restling.Core
{
    public class RestRequest<T>(string uri,
                                HttpMethod method,
                                RequestHeaders headers,
                                string? customMethod = null)
        : RestRequest(uri, 
                      method,
                      headers,
                      customMethod)
    {
        #region Fields

        private T? requestData;

        #endregion

        #region Properties

        public T? RequestData
        {
            get => this.requestData;
            set => this.requestData = value;
        }

        #endregion

        #region .ctor

        public RestRequest(string uri,
                           HttpMethod method,
                           T? requestData,
                           string? customMethod = null)
            : this(uri, method, requestData, new(), customMethod)
        {
        }

        public RestRequest(string uri,
                           HttpMethod method,
                           T? requestData,
                           RequestHeaders headers,
                           string? customMethod = null)
            : this(uri, method, headers, customMethod)
        {
            this.requestData = requestData;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{this.Method}[{this.CustomMethod}] {this.Uri} {this.Headers} {this.RequestData}";
        }

        #endregion
    }
}
