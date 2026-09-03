using System.Net;

namespace AMDevIT.Restling.Core.Network
{
    /// <summary>Owns and reuses one alternative native transport per immutable request proxy selection.</summary>
    internal sealed class RequestTransportPool : IDisposable
    {
        #region Fields

        private readonly HttpClient defaultClient;
        private readonly CookieContainer cookieContainer;
        private readonly Func<CookieContainer, HttpMessageHandler>? handlerFactory;
        private readonly Dictionary<RequestProxyOptions, HttpClient> clients = [];
        private bool disposed;

        #endregion

        #region .ctor

        /// <summary>Creates a pool that borrows the default client and owns only clients created by its factory.</summary>
        public RequestTransportPool(HttpClient defaultClient,
                                    CookieContainer cookieContainer,
                                    Func<CookieContainer, HttpMessageHandler>? handlerFactory)
        {
            this.defaultClient = defaultClient;
            this.cookieContainer = cookieContainer;
            this.handlerFactory = handlerFactory;
        }

        #endregion

        #region Methods

        /// <summary>Returns the context client or a cached client matching the request override.</summary>
        public HttpClient Resolve(RequestProxyOptions? options)
        {
            RequestProxyOptions selection = options ?? RequestProxyOptions.Default;

            if (selection.Mode == RequestProxyMode.Default)
                return this.defaultClient;
            lock (this.clients)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (!this.clients.TryGetValue(selection, out HttpClient? client))
                {
                    client = this.CreateClient(selection);
                    this.clients.Add(selection, client);
                }
                return client;
            }
        }

        /// <summary>Disposes every alternative client and its owned handler.</summary>
        public void Dispose()
        {
            lock (this.clients)
            {
                if (this.disposed)
                    return;
                foreach (HttpClient client in this.clients.Values)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                this.clients.Clear();
                this.disposed = true;
            }
        }

        /// <summary>Creates and configures one owned alternative client.</summary>
        private HttpClient CreateClient(RequestProxyOptions options)
        {
            HttpMessageHandler handler;
            HttpClient client;

            if (this.handlerFactory == null)
                throw new NotSupportedException("This HttpClientContext has no request handler factory. Register one with AddRequestHandlerFactory before using Direct or Custom request proxy options.");
            handler = this.handlerFactory(this.cookieContainer) ??
                      throw new InvalidOperationException("The request handler factory returned null.");
            try
            {
                ConfigureHandler(handler, this.cookieContainer, options);
                client = new HttpClient(handler, disposeHandler: true)
                {
                    BaseAddress = this.defaultClient.BaseAddress,
                    DefaultRequestVersion = this.defaultClient.DefaultRequestVersion,
                    DefaultVersionPolicy = this.defaultClient.DefaultVersionPolicy,
                    MaxResponseContentBufferSize = this.defaultClient.MaxResponseContentBufferSize,
                    Timeout = this.defaultClient.Timeout
                };
                foreach (KeyValuePair<string, IEnumerable<string>> header in this.defaultClient.DefaultRequestHeaders)
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                return client;
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>Applies routing while ensuring every alternative native handler shares the context cookie jar.</summary>
        private static void ConfigureHandler(HttpMessageHandler handler,
                                             CookieContainer cookieContainer,
                                             RequestProxyOptions options)
        {
            IWebProxy? proxy = options.Mode == RequestProxyMode.Custom ? new WebProxy(options.ProxyUri!) : null;
            ICredentials? credentials;

            switch (handler)
            {
                case SocketsHttpHandler socketsHttpHandler:
                    credentials = socketsHttpHandler.Proxy?.Credentials;
                    if (proxy != null)
                        proxy.Credentials = credentials;
                    socketsHttpHandler.CookieContainer = cookieContainer;
                    socketsHttpHandler.UseCookies = true;
                    socketsHttpHandler.Proxy = proxy;
                    socketsHttpHandler.UseProxy = options.Mode == RequestProxyMode.Custom;
                    socketsHttpHandler.AllowAutoRedirect = options.AllowAutoRedirect;
                    break;

                case HttpClientHandler httpClientHandler:
                    credentials = httpClientHandler.Proxy?.Credentials;
                    if (proxy != null)
                        proxy.Credentials = credentials;
                    httpClientHandler.CookieContainer = cookieContainer;
                    httpClientHandler.UseCookies = true;
                    httpClientHandler.Proxy = proxy;
                    httpClientHandler.UseProxy = options.Mode == RequestProxyMode.Custom;
                    httpClientHandler.AllowAutoRedirect = options.AllowAutoRedirect;
                    break;

                default:
                    throw new NotSupportedException("The request handler factory must return a SocketsHttpHandler or HttpClientHandler.");
            }
        }

        #endregion
    }
}
