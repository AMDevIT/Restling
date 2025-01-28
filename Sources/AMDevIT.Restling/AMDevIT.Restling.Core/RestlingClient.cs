using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AMDevIT.Restling.Core
{
    public class RestlingClient(HttpClient httpClient,
                                ILogger? logger)
        : IRestlingClient, IDisposable
    {
        #region Fields

        private readonly HttpClient httpClient = httpClient;
        private readonly ILogger? logger = logger;
        private bool disposedValue;

        #endregion

        #region Properties

        public bool DisposeHttpClient
        {
            get;
            set;
        }

        public bool Disposed => this.disposedValue;

        protected HttpClient HttpClientInstance => this.httpClient;
        protected ILogger? Logger => this.logger;

        #endregion

        #region .ctor

        public RestlingClient()
            : this(new HttpClient(), null) 
        {

        }

        public RestlingClient(ILogger logger)
          : this(new HttpClient(), logger)
        {

        }

        #endregion

        #region Methods

        public async Task<RestRequestResult<T>> GetAsync<T>(string uri, CancellationToken cancellationToken = default)          
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);

            restRequest = new RestRequest(uri,
                                          HttpMethod.GET,
                                          null);

            try
            {
                this.Logger?.LogDebug("Executing GET REST request to {uri}", uri);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClient.GetAsync(uri, cancellationToken);
                stopwatch.Stop();
                this.Logger?.LogDebug("GET REST request to {uri} executed in {elapsed} ms", uri, stopwatch.ElapsedMilliseconds);
            }
            catch(Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                this.Logger?.LogError(exc, "Cannot execute GET REST request.");
                return new RestRequestResult<T>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }
            
            restRequestResult = await httpResponseParser.DecodeAsync<T>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }    

        public async Task<RestRequestResult<T>> PostAsync<D, T>(string uri, D requestData, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest<D>(uri,
                                             HttpMethod.POST,
                                             requestData);
            try
            {
                HttpContent content = this.BuildHttpContent(requestData);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClient.PostAsync(uri, content, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute POST REST request.");
                return new RestRequestResult<T>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync<T>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        public Task<RestRequestResult> ExecuteRequestAsync(RestRequest restRequest, 
                                                           CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.DisposeHttpClient)
                    {
                        this.httpClient.Dispose();
                    }
                }
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {   
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }      

        private HttpContent BuildHttpContent<T>(T requestData)
        {
            HttpContent content;
            if (requestData == null)
                content = new StringContent(string.Empty, Encoding.UTF8, HttpMediaType.ApplicationJson);
            else
            {
                JsonSerialization jsonSerialization = new(this.Logger);
                string jsonContent = jsonSerialization.Serialize(requestData);
                content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            return content;
        }

        #endregion
    }
}
