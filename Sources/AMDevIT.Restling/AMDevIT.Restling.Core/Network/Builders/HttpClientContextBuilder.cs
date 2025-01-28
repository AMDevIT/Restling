using AMDevIT.Restling.Core.Cookies;
using System.Collections.ObjectModel;
using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    /// <summary>
    /// Builds a valid <see cref="HttpClientContext"/> to use with a Restling instance.
    /// </summary>
    public class HttpClientContextBuilder : IHttpClientContextBuilder
    {
        #region  Consts

        protected const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

        #endregion

        #region Fields

        private HttpMessageHandler? httpMessageHandler;
        private CookieContainer? cookieContainer;
        private bool disposeHandler = false;
        private string userAgent = DefaultUserAgent;

        private readonly HashSet<HttpCookieData> cookies = [];
        private readonly Dictionary<string, string> defaultHeaders = [];
        private TimeSpan? timeout = null;

        #endregion

        #region Properties

        public ReadOnlyCollection<HttpCookieData> Cookies => this.cookies.ToList().AsReadOnly();

        #endregion

        #region Methods

        public HttpClientContextBuilder AddCookieContainer(CookieContainer cookieContainer)
        {
            this.cookieContainer = cookieContainer;

            if (this.httpMessageHandler != null)
            {
                switch (this.httpMessageHandler)
                {
                    case SocketsHttpHandler socketsHttpHandler:
                        socketsHttpHandler.CookieContainer = this.cookieContainer;
                        break;

                    case HttpClientHandler httpClientHandler:
                        httpClientHandler.CookieContainer = this.cookieContainer;
                        break;
                }
            }
            return this;
        }

        public HttpClientContextBuilder ClearCookieContainer()
        {
            return this.AddCookieContainer(new());
        }


        public HttpClientContextBuilder AddCookie(HttpCookieData cookie)
        {
            this.cookies.Add(cookie);
            return this;
        }

        public HttpClientContextBuilder AddCookies(IEnumerable<HttpCookieData> cookies)
        {
            foreach (HttpCookieData cookieData in cookies)
            {
                this.cookies.Add(cookieData);
            }

            return this;
        }

        public HttpClientContextBuilder RemoveCookie(HttpCookieData cookie)
        {
            this.cookies.Remove(cookie);
            return this;
        }

        public HttpClientContextBuilder RemoveCookies(IEnumerable<HttpCookieData> cookies)
        {
            foreach (HttpCookieData cookieData in cookies)
            {
                this.cookies.Remove(cookieData);
            }
            return this;
        }

        public HttpClientContextBuilder ClearCookies()
        {
            this.cookies.Clear();
            return this;
        }

        public HttpClientContextBuilder AddHandler(HttpMessageHandler handler, bool diposeHandler = false)
        {
            this.httpMessageHandler = handler;
            this.disposeHandler = diposeHandler;

            return this;
        }

        public HttpClientContextBuilder AddUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                this.userAgent = DefaultUserAgent;
            else
                this.userAgent = userAgent;

            return this;
        }

        public HttpClientContextBuilder AddDefaultHeader(string name, string value)
        {
            this.defaultHeaders[name] = value;
            return this;
        }

        public HttpClientContextBuilder RemoveDefaultHeader(string name)
        {
            this.defaultHeaders.Remove(name);
            return this;
        }

        public HttpClientContextBuilder SetTimeout(TimeSpan? timeout)
        {
            this.timeout = timeout;
            return this;
        }

        public HttpClientContextBuilder ConfigureHandler(Action<HttpMessageHandler> configureHandler)
        {
            ArgumentNullException.ThrowIfNull(configureHandler, nameof(configureHandler));

            if (this.httpMessageHandler == null)
            {
                this.httpMessageHandler = new SocketsHttpHandler();
                this.disposeHandler = true;
            }

            configureHandler(this.httpMessageHandler);
            return this;
        }

        public HttpClientContext Build()
        {
            HttpClient httpClient;
            HttpClientContext httpClientContext;

            this.cookieContainer ??= new CookieContainer();

            if (this.httpMessageHandler == null)
            {
                SocketsHttpHandler socketsHttpHandler = new()
                {
                    CookieContainer = this.cookieContainer
                };
                this.httpMessageHandler = socketsHttpHandler;
                this.disposeHandler = true;
            }

            if (this.cookies.Count > 0)
            {
                foreach (HttpCookieData cookieData in this.cookies)
                {
                    Cookie cookie = new(cookieData.Name, cookieData.Value, cookieData.Path, cookieData.Domain);
                    this.cookieContainer.Add(cookie);
                }
            }

            httpClient = new(this.httpMessageHandler, this.disposeHandler);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(this.userAgent);

            if (this.timeout.HasValue)
                httpClient.Timeout = this.timeout.Value;

            foreach (KeyValuePair<string, string> header in this.defaultHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            httpClientContext = new(httpClient, this.httpMessageHandler, this.cookieContainer);

            return httpClientContext;
        }


        #endregion
    }
}
