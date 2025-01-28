using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    public class HttpClientContext(HttpClient httpClient,
                                   HttpMessageHandler httpMessageHandler,
                                   CookieContainer cookieContainer)
        : IDisposable
    {
        #region Fields

        private readonly HttpClient httpClient = httpClient;
        private readonly HttpMessageHandler httpMessageHandler = httpMessageHandler;
        private readonly CookieContainer cookieContainer = cookieContainer;
        private bool disposedValue;

        #endregion

        #region Properties

        public bool Disposed => this.disposedValue;

        #endregion

        #region Properties

        public HttpClient HttpClient => this.httpClient;
        public HttpMessageHandler HttpMessageHandler => this.httpMessageHandler;
        public CookieContainer CookieContainer => this.cookieContainer;

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        this.HttpMessageHandler.Dispose();                        
                    }
                    catch(Exception)
                    {

                    }

                    try
                    {
                        this.HttpClient.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
