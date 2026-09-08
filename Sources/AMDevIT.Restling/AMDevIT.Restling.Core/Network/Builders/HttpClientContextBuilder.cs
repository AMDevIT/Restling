using AMDevIT.Restling.Core.Cookies;
using System.Collections.ObjectModel;
using AMDevIT.Restling.Core.Cookies.Storage;
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Network;
using System.Net;
using System.Net.Http.Headers;

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
        private CookieContainer? fallbackCookieContainer;
        private HttpMessageHandlerOwnership handlerOwnership = HttpMessageHandlerOwnership.Borrowed;
        private string userAgent = DefaultUserAgent;

        private readonly HashSet<HttpCookieData> cookies = [];
        private readonly Dictionary<string, string> defaultHeaders = [];
        private AuthenticationHeader? authenticationHeader = null;
        private TimeSpan? timeout = null;
        private ContentCodecRegistry codecs = new();
        private WebProxy? proxy;
        private bool allowAutoRedirect;
        private Func<CookieContainer, HttpMessageHandler>? requestHandlerFactory;
        private bool usesDefaultRequestHandlerFactory;
        private ICookiesStorageProvider? cookieStorageProvider;

        #endregion

        #region Properties

        public ReadOnlyCollection<HttpCookieData> Cookies => this.cookies.ToList().AsReadOnly();

        #endregion

        #region Methods

        /// <summary>Adds a codec with priority over previously registered codecs, retaining the defaults.</summary>
        public HttpClientContextBuilder AddCodec(IContentCodec codec)
        {
            this.codecs = this.codecs.WithCodec(codec);
            return this;
        }

        #region Cookies

        /// <summary>Selects an explicit cookie container and enables cookies on a supported native handler.</summary>
        public HttpClientContextBuilder AddCookieContainer(CookieContainer cookieContainer)
        {
            ArgumentNullException.ThrowIfNull(cookieContainer);
            this.cookieStorageProvider = null;
            this.cookieContainer = cookieContainer;
            this.ResolveCookieContainer(enableCookies: true);
            return this;
        }

        /// <summary>Selects the cookie storage provider whose live container is shared with the HTTP transport.</summary>
        /// <param name="cookieStorageProvider">The provider owned by the generated context.</param>
        /// <returns>The current builder instance.</returns>
        public HttpClientContextBuilder AddCookieStorageProvider(ICookiesStorageProvider cookieStorageProvider)
        {
            ArgumentNullException.ThrowIfNull(cookieStorageProvider);
            this.cookieStorageProvider = cookieStorageProvider;
            this.cookieContainer = cookieStorageProvider.CookieContainer;
            this.ResolveCookieContainer(enableCookies: true);
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

        #endregion

        #region Handlers

        public HttpClientContextBuilder AddHandler(HttpMessageHandler handler, bool diposeHandler = false)
        {
            return this.AddHandler(handler,
                                   diposeHandler
                                       ? HttpMessageHandlerOwnership.Owned
                                       : HttpMessageHandlerOwnership.Borrowed);
        }

        /// <summary>Adds a handler with an explicit ownership contract.</summary>
        /// <param name="handler">The message handler used by the generated HTTP client.</param>
        /// <param name="ownership">Whether the generated context borrows or owns the handler.</param>
        /// <returns>The current builder instance.</returns>
        public HttpClientContextBuilder AddHandler(HttpMessageHandler handler, HttpMessageHandlerOwnership ownership)
        {
            ArgumentNullException.ThrowIfNull(handler);
            if (!Enum.IsDefined(ownership))
                throw new ArgumentOutOfRangeException(nameof(ownership));

            if (this.proxy != null)
                ApplyProxy(handler, this.proxy, this.allowAutoRedirect);

            this.httpMessageHandler = handler;
            this.handlerOwnership = ownership;
            this.usesDefaultRequestHandlerFactory = false;
            this.ResolveCookieContainer(enableCookies: true);

            return this;
        }

        public HttpClientContextBuilder ConfigureHandler(Action<HttpMessageHandler> configureHandler)
        {
            ArgumentNullException.ThrowIfNull(configureHandler, nameof(configureHandler));

            if (this.httpMessageHandler == null)
            {
                this.httpMessageHandler = new SocketsHttpHandler();
                this.handlerOwnership = HttpMessageHandlerOwnership.Owned;
                this.ResolveCookieContainer(enableCookies: true);
                if (this.proxy != null)
                    ApplyProxy(this.httpMessageHandler, this.proxy, this.allowAutoRedirect);
            }

            configureHandler(this.httpMessageHandler);
            this.usesDefaultRequestHandlerFactory = false;
            return this;
        }

        /// <summary>Selects an explicit proxy and HTTP redirect policy for a native handler.</summary>
        /// <param name="proxyUri">An absolute HTTP, HTTPS, SOCKS4, SOCKS4a, or SOCKS5 proxy URI without credentials, query, fragment, or a non-root path.</param>
        /// <param name="allowAutoRedirect">Whether the handler automatically follows HTTP response redirects.</param>
        /// <returns>The current builder instance.</returns>
        /// <exception cref="ArgumentException">The proxy URI is invalid or unsupported.</exception>
        /// <exception cref="NotSupportedException">The selected handler is not a directly supplied native handler.</exception>
        /// <exception cref="InvalidOperationException">The selected handler has already started processing requests.</exception>
        /// <remarks>Configure before sending requests. Credentials can be set through ConfigureHandler. Later ConfigureHandler changes are retained by Build.</remarks>
        public HttpClientContextBuilder AddProxy(string proxyUri, bool allowAutoRedirect)
        {
            Uri address;
            WebProxy selectedProxy;

            address = ProxyUriParser.Parse(proxyUri);
            selectedProxy = new WebProxy(address);
            if (this.httpMessageHandler != null)
                ApplyProxy(this.httpMessageHandler, selectedProxy, allowAutoRedirect);
            this.proxy = selectedProxy;
            this.allowAutoRedirect = allowAutoRedirect;
            return this;
        }

        /// <summary>Registers a factory for transports used by Direct and Custom per-request proxy overrides.</summary>
        /// <param name="handlerFactory">Creates a fresh handler and receives the context's shared cookie container.</param>
        /// <returns>The current builder instance.</returns>
        /// <remarks>The generated handlers are owned by the context. The factory must return a directly supported native handler.</remarks>
        public HttpClientContextBuilder AddRequestHandlerFactory(Func<CookieContainer, HttpMessageHandler> handlerFactory)
        {
            ArgumentNullException.ThrowIfNull(handlerFactory);
            this.requestHandlerFactory = handlerFactory;
            return this;
        }

        #endregion

        #region Headers

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

        public HttpClientContextBuilder ClearDefaultHeaders()
        {
            this.defaultHeaders.Clear();
            return this;
        }

        public HttpClientContextBuilder AddAuthenticationHeader(string scheme, string parameter)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(scheme, "Authentication scheme cannot be null");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(parameter, "Authentication parameter cannot be null");
            AuthenticationHeader authenticationHeader = new(scheme, parameter);
            this.authenticationHeader = authenticationHeader;
            return this;
        }

        public HttpClientContextBuilder AddAuthenticationHeader(AuthenticationHeaderValue authenticationHeaderValue)
        {
            ArgumentNullException.ThrowIfNull(authenticationHeaderValue, "Authentication header cannot be null");
            AuthenticationHeader authenticationHeader = new(authenticationHeaderValue.Scheme, 
                                                            authenticationHeaderValue.Parameter ?? string.Empty);
            this.authenticationHeader = authenticationHeader;
            return this;
        }

        public HttpClientContextBuilder AddAuthenticationHeader(AuthenticationHeader authenticationHeader)
        {
            ArgumentNullException.ThrowIfNull(authenticationHeader, "Authentication header cannot be null");
            this.authenticationHeader = authenticationHeader;
            return this;
        }

        public HttpClientContextBuilder RemoveAuthenticationHeader()
        {
            this.authenticationHeader = null;
            return this;
        }

        #endregion

        #region REST parameters

        public HttpClientContextBuilder SetTimeout(TimeSpan? timeout)
        {
            this.timeout = timeout;
            return this;
        }

        #endregion

        public HttpClientContext Build()
        {
            HttpClient httpClient;
            HttpClientContext httpClientContext;
            HttpClientContextOwnership ownership;
            CookieContainer effectiveCookieContainer;
            ICookiesStorageProvider effectiveCookieStorageProvider;

            if (this.httpMessageHandler == null)
            {
                SocketsHttpHandler socketsHttpHandler = new()
                {
                    UseCookies = true,
                    AllowAutoRedirect = false
                };
                this.httpMessageHandler = socketsHttpHandler;
                this.handlerOwnership = HttpMessageHandlerOwnership.Owned;
                this.usesDefaultRequestHandlerFactory = true;
                if (this.proxy != null)
                    ApplyProxy(this.httpMessageHandler, this.proxy, this.allowAutoRedirect);
            }

            effectiveCookieContainer = this.ResolveCookieContainer();
            effectiveCookieStorageProvider = this.cookieStorageProvider ?? new CookieStorageProvider(effectiveCookieContainer);

            if (this.cookies.Count > 0)
            {
                foreach (HttpCookieData cookieData in this.cookies)
                {
                    Cookie cookie = new(cookieData.Name, cookieData.Value, cookieData.Path, cookieData.Domain);
                    effectiveCookieContainer.Add(cookie);
                }
            }           

            httpClient = new(this.httpMessageHandler, disposeHandler: false);
            ownership = HttpClientContextOwnership.HttpClient;
            if (this.handlerOwnership == HttpMessageHandlerOwnership.Owned)
                ownership |= HttpClientContextOwnership.HttpMessageHandler;

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(this.userAgent);

            if (this.timeout.HasValue)
                httpClient.Timeout = this.timeout.Value;

            foreach (KeyValuePair<string, string> header in this.defaultHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            if (this.authenticationHeader != null)
            {
                AuthenticationHeaderValue authenticationHeaderValue;
                authenticationHeaderValue = new (this.authenticationHeader.Scheme, this.authenticationHeader.Parameter);
                httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
            }

            httpClientContext = new(httpClient,
                                    this.httpMessageHandler,
                                    effectiveCookieContainer,
                                    ownership,
                                    this.requestHandlerFactory ?? (this.usesDefaultRequestHandlerFactory ? CreateDefaultRequestHandler : null),
                                    effectiveCookieStorageProvider)
            {
                Codecs = this.codecs
            };

            return httpClientContext;
        }

        /// <summary>Creates the native baseline used by per-request proxy overrides for a default builder.</summary>
        private static HttpMessageHandler CreateDefaultRequestHandler(CookieContainer cookieContainer)
        {
            return new SocketsHttpHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false
            };
        }

        /// <summary>Applies explicit proxy settings without replacing the handler or its cookie container.</summary>
        private static void ApplyProxy(HttpMessageHandler handler, WebProxy proxy, bool allowAutoRedirect)
        {
            switch (handler)
            {
                case SocketsHttpHandler socketsHttpHandler:
                    if (!ReferenceEquals(socketsHttpHandler.Proxy, proxy))
                        socketsHttpHandler.Proxy = proxy;
                    if (!socketsHttpHandler.UseProxy)
                        socketsHttpHandler.UseProxy = true;
                    if (socketsHttpHandler.AllowAutoRedirect != allowAutoRedirect)
                        socketsHttpHandler.AllowAutoRedirect = allowAutoRedirect;
                    break;

                case HttpClientHandler httpClientHandler:
                    if (!ReferenceEquals(httpClientHandler.Proxy, proxy))
                        httpClientHandler.Proxy = proxy;
                    if (!httpClientHandler.UseProxy)
                        httpClientHandler.UseProxy = true;
                    if (httpClientHandler.AllowAutoRedirect != allowAutoRedirect)
                        httpClientHandler.AllowAutoRedirect = allowAutoRedirect;
                    break;

                default:
                    throw new NotSupportedException("AddProxy requires a directly supplied SocketsHttpHandler or HttpClientHandler. Configure custom or delegating handlers explicitly.");
            }
        }

        /// <summary>Shares the explicit or native cookie container without replacing existing handler state unnecessarily.</summary>
        /// <remarks>An explicit container takes precedence. Without one, native cookie settings and stored cookies are retained.</remarks>
        private CookieContainer ResolveCookieContainer(bool enableCookies = false)
        {
            switch (this.httpMessageHandler)
            {
                case SocketsHttpHandler socketsHttpHandler:
                    if (this.cookieContainer != null)
                    {
                        if (!ReferenceEquals(socketsHttpHandler.CookieContainer, this.cookieContainer))
                            socketsHttpHandler.CookieContainer = this.cookieContainer;
                        if (enableCookies && !socketsHttpHandler.UseCookies)
                            socketsHttpHandler.UseCookies = true;
                    }
                    return socketsHttpHandler.CookieContainer;

                case HttpClientHandler httpClientHandler:
                    if (this.cookieContainer != null)
                    {
                        if (!ReferenceEquals(httpClientHandler.CookieContainer, this.cookieContainer))
                            httpClientHandler.CookieContainer = this.cookieContainer;
                        if (enableCookies && !httpClientHandler.UseCookies)
                            httpClientHandler.UseCookies = true;
                    }
                    return httpClientHandler.CookieContainer;

                default:
                    return this.cookieContainer ?? (this.fallbackCookieContainer ??= new CookieContainer());
            }
        }


        #endregion
    }
}
