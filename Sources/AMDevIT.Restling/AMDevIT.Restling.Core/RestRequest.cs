using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Serialization;

namespace AMDevIT.Restling.Core
{
    public class RestRequest
    {
        #region Fields

        protected readonly RequestHeaders headers = new();
        private string uri = null!;
        private HttpMethod method;
        private string? customMethod;

        #endregion

        #region Properties

        public string Uri
        {
            get => this.uri;
            set
            {
                if (!this.AllowUnsafeURIs && !this.IsValidUri(value))
                    throw new ArgumentException($"Blocked or invalid URI: {value}");
                this.uri = value;
            }
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

        public bool AllowUnsafeURIs
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Defines a custom URI validator delegate, if you want to apply custom validation logic.
        /// </summary>
        public Func<string, bool>? CustomUriValidatorDelegate
        {
            get;
            set;
        } = null;

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
                           string? customMethod = null)
        {
            this.Uri = uri;
            this.method = method;
            this.customMethod = customMethod;
        }

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

        protected virtual bool IsValidUri(string uri)
        {
            if (this.CustomUriValidatorDelegate != null)
                return this.CustomUriValidatorDelegate(uri);

            bool uriParsed;

            uriParsed = System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed);
            return uriParsed;
        }

        public override string ToString()
        {
            return $"{this.Method}[{this.CustomMethod}] {this.Uri} {this.Headers}";
        }

        #endregion
    }
}
