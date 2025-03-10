using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Serialization;

namespace AMDevIT.Restling.Core
{
    public class RestRequest(string uri,
                             HttpMethod method,
                             string? customMethod = null)
    {
        #region Fields

        protected readonly RequestHeaders headers = new();
        private string uri = uri;
        private HttpMethod method = method;
        private string? customMethod = customMethod;

        #endregion

        #region Properties

        public string Uri
        {
            get => this.uri;
            set => this.uri = value;
        }

        public HttpMethod Method
        {
            get => this.method;
            set => this.method = value;
        }

        public string? CustomMethod
        {
            get => this.customMethod;
            set => this.customMethod = value;
        }

        /// <summary>
        /// Forces the selected serialization library to be used when serializing/deserializing payloads.
        /// </summary>
        public PayloadJsonSerializerLibrary? ForcePayloadJsonSerializerLibrary
        {
            get;
            set;
        } = null;

        public RequestHeaders Headers => this.headers;

        #endregion

        #region .ctor

        public RestRequest(string uri,
                           HttpMethod method,
                           RequestHeaders headers,
                           string? customMethod = null)
            : this(uri, method, customMethod)
        {
            this.headers = headers;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{this.Method}[{this.CustomMethod}] {this.Uri} {this.Headers}";
        }

        #endregion
    }
}
