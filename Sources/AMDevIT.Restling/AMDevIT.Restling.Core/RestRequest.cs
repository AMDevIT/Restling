namespace AMDevIT.Restling.Core
{
    public class RestRequest(string uri,
                             HttpMethod method,
                             string? customMethod = null)
    {
        #region Fields

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

        #endregion
    }
}
