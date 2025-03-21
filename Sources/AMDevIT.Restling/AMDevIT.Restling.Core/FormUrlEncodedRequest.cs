using AMDevIT.Restling.Core.Network;

namespace AMDevIT.Restling.Core
{
    /// <summary>
    /// Represents a request that will be sent as a non rest compliant form-urlencoded request.
    /// </summary>
    public class FormUrlEncodedRequest
        : RestRequest
    {
        #region Fields

        private readonly IDictionary<string, string> parameters = new Dictionary<string, string>();

        #endregion

        #region Properties

        public IDictionary<string, string> Parameters => this.parameters;

        #endregion

        #region .ctor

        public FormUrlEncodedRequest(string uri, 
                                     HttpMethod method, 
                                     string? customMethod = null) 
            : base(uri, method, customMethod)
        {
        }

        public FormUrlEncodedRequest(string uri, 
                                     HttpMethod method, 
                                     RequestHeaders headers, 
                                     string? customMethod = null) 
            : base(uri, method, headers, customMethod)
        {
        }

        public FormUrlEncodedRequest(string uri,
                                     HttpMethod method,
                                     IDictionary<string, string> parameters,
                                     string? customMethod = null)
            : this(uri, method, customMethod)
        {
            this.parameters = parameters;
        }

        public FormUrlEncodedRequest(string uri,
                                     HttpMethod method,
                                     RequestHeaders headers,
                                     IDictionary<string, string> parameters,
                                     string? customMethod = null)
            : this(uri, method, headers, customMethod)
        {
            this.parameters = parameters;
        }

        #endregion
    }
}
