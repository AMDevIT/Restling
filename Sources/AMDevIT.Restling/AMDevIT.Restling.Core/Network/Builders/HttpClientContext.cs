using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    using AMDevIT.Restling.Core.Codecs;

    public class HttpClientContext : IDisposable
    {
        #region Fields

        private readonly HttpClient httpClient;
        private readonly HttpMessageHandler httpMessageHandler;
        private readonly CookieContainer cookieContainer;
        private readonly HttpClientContextOwnership ownership;
        private bool disposedValue;

        #endregion

        #region Properties

        public bool Disposed => this.disposedValue;

        /// <summary>Gets the codec snapshot shared by clients using this context.</summary>
        public ContentCodecRegistry Codecs { get; init; } = new();

        public HttpClient HttpClient => this.httpClient;
        public HttpMessageHandler HttpMessageHandler => this.httpMessageHandler;
        public CookieContainer CookieContainer => this.cookieContainer;
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
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(httpMessageHandler);
            ArgumentNullException.ThrowIfNull(cookieContainer);
            if ((ownership & ~HttpClientContextOwnership.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(ownership));

            this.httpClient = httpClient;
            this.httpMessageHandler = httpMessageHandler;
            this.cookieContainer = cookieContainer;
            this.ownership = ownership;
        }

        #endregion

        #region Methods

        /// <summary>Disposes only the resources declared by Ownership.</summary>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases resources owned by the context.</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
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
                }

                this.disposedValue = true;
            }
        }

        #endregion
    }
}
