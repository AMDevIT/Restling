using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    public class HttpClientBuilder
    {
        #region Fields

        private HttpMessageHandler? httpMessageHandler;
        private CookieContainer? cookieContainer;
        private bool disposeHandler = false;

        #endregion

        #region Methods

        public HttpClientBuilder AddCookieContainer(CookieContainer cookieContainer)
        {
            this.cookieContainer = cookieContainer;
            return this;
        }

        public HttpClientBuilder AddHandler(HttpMessageHandler handler, bool diposeHandler = false)
        {
            this.httpMessageHandler = handler;
            this.disposeHandler = diposeHandler;

            return this;
        }

        public HttpClient Build()
        {
            HttpClient httpClient;

            if (this.httpMessageHandler != null)
                httpClient = new(this.httpMessageHandler, this.disposeHandler);
            else
                httpClient = new()

            return httpClient;
        }


        #endregion
    }
}
