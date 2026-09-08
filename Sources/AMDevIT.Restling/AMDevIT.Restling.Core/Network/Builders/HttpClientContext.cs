using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    using AMDevIT.Restling.Core.Codecs;
    using AMDevIT.Restling.Core.Cookies.Storage;

    public class HttpClientContext : IDisposable
    {
        #region Fields

        private readonly HttpClient httpClient;
        private readonly HttpMessageHandler httpMessageHandler;
        private readonly CookieContainer cookieContainer;
        private readonly HttpClientContextOwnership ownership;
        private readonly RequestTransportPool requestTransports;
        private readonly ICookiesStorageProvider cookieStorageProvider;
        private readonly object cookieStorageSync = new();
        private Task? cookieStorageLoadTask;
        private bool disposedValue;

        #endregion

        #region Properties

        public bool Disposed => this.disposedValue;

        /// <summary>Gets the codec snapshot shared by clients using this context.</summary>
        public ContentCodecRegistry Codecs { get; init; } = new();

        public HttpClient HttpClient => this.httpClient;
        public HttpMessageHandler HttpMessageHandler => this.httpMessageHandler;
        public CookieContainer CookieContainer => this.cookieContainer;
        public ICookiesStorageProvider CookieStorageProvider => this.cookieStorageProvider;
        public HttpClientContextOwnership Ownership => this.ownership;

        #endregion

        #region .ctor

        /// <summary>Creates a context that preserves the historical ownership of both resources.</summary>
        public HttpClientContext(HttpClient httpClient,
                                 HttpMessageHandler httpMessageHandler,
                                 CookieContainer cookieContainer)
            : this(httpClient, httpMessageHandler, cookieContainer, HttpClientContextOwnership.All)
        {
        }

        /// <summary>Creates a context with explicit resource ownership.</summary>
        public HttpClientContext(HttpClient httpClient,
                                 HttpMessageHandler httpMessageHandler,
                                 CookieContainer cookieContainer,
                                 HttpClientContextOwnership ownership)
            : this(httpClient, httpMessageHandler, cookieContainer, ownership, null)
        {
        }

        /// <summary>Creates a context with explicit ownership and an optional factory for per-request proxy transports.</summary>
        /// <param name="httpClient">The default client used when a request has no transport override.</param>
        /// <param name="httpMessageHandler">The handler associated with the default client.</param>
        /// <param name="cookieContainer">The cookie jar shared by alternative transports.</param>
        /// <param name="ownership">The ownership of default transport resources.</param>
        /// <param name="requestHandlerFactory">An optional factory producing fresh, context-owned native handlers.</param>
        public HttpClientContext(HttpClient httpClient,
                                 HttpMessageHandler httpMessageHandler,
                                 CookieContainer cookieContainer,
                                 HttpClientContextOwnership ownership,
                                 Func<CookieContainer, HttpMessageHandler>? requestHandlerFactory)
            : this(httpClient,
                   httpMessageHandler,
                   cookieContainer,
                   ownership,
                   requestHandlerFactory,
                   new CookieStorageProvider(cookieContainer))
        {
        }

        /// <summary>Creates a context with transport ownership, request routing, and cookie persistence.</summary>
        public HttpClientContext(HttpClient httpClient,
                                 HttpMessageHandler httpMessageHandler,
                                 CookieContainer cookieContainer,
                                 HttpClientContextOwnership ownership,
                                 Func<CookieContainer, HttpMessageHandler>? requestHandlerFactory,
                                 ICookiesStorageProvider cookieStorageProvider)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(httpMessageHandler);
            ArgumentNullException.ThrowIfNull(cookieContainer);
            ArgumentNullException.ThrowIfNull(cookieStorageProvider);
            if (!ReferenceEquals(cookieContainer, cookieStorageProvider.CookieContainer))
                throw new ArgumentException("The cookie storage provider must expose the context cookie container.", nameof(cookieStorageProvider));
            if ((ownership & ~HttpClientContextOwnership.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(ownership));

            this.httpClient = httpClient;
            this.httpMessageHandler = httpMessageHandler;
            this.cookieContainer = cookieContainer;
            this.cookieStorageProvider = cookieStorageProvider;
            this.ownership = ownership;
            this.requestTransports = new RequestTransportPool(httpClient, cookieContainer, requestHandlerFactory);
        }

        #endregion

        #region Methods

        /// <summary>Disposes only the resources declared by Ownership.</summary>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Resolves the reusable transport selected for an individual request.</summary>
        public HttpClient ResolveHttpClient(RequestProxyOptions? options)
        {
            return this.requestTransports.Resolve(options);
        }

        /// <summary>Loads the selected cookie storage once before the first HTTP request.</summary>
        internal Task EnsureCookieStorageLoadedAsync(CancellationToken cancellationToken)
        {
            Task loadTask;

            lock (this.cookieStorageSync)
            {
                this.cookieStorageLoadTask ??= this.CookieStorageProvider.LoadAsync(CancellationToken.None);
                loadTask = this.cookieStorageLoadTask;
            }
            return loadTask.WaitAsync(cancellationToken);
        }

        /// <summary>Signals that the live cookie jar may have changed.</summary>
        internal void NotifyCookiesChanged()
        {
            this.CookieStorageProvider.NotifyCookiesChanged();
        }

        /// <summary>Invalidates a failed request-specific transport without affecting the default client.</summary>
        internal void InvalidateHttpClient(RequestProxyOptions? options, HttpClient failedClient)
        {
            this.requestTransports.Invalidate(options, failedClient);
        }

        /// <summary>Releases resources owned by the context.</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.requestTransports.Dispose();
                    if (this.Ownership.HasFlag(HttpClientContextOwnership.HttpClient))
                    {
                        try
                        {
                            this.HttpClient.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (this.Ownership.HasFlag(HttpClientContextOwnership.HttpMessageHandler))
                    {
                        try
                        {
                            this.HttpMessageHandler.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (this.CookieStorageProvider is IDisposable disposableCookieStorage)
                        disposableCookieStorage.Dispose();
                    else if (this.CookieStorageProvider is IAsyncDisposable asyncDisposableCookieStorage)
                        asyncDisposableCookieStorage.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }

                this.disposedValue = true;
            }
        }

        #endregion
    }
}
