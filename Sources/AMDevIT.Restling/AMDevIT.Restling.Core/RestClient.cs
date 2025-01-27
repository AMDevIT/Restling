using AMDevIT.Restling.Core.Common;
using AMDevIT.Restling.Core.Text;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Core
{
    public class RestClient
        : IRestClient, IDisposable
    {
        #region Fields

        private readonly HttpClient httpClient;
        private readonly ILogger? logger;
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

        public RestClient(HttpClient httpClient, 
                          ILogger? logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public RestClient()
            : this(new HttpClient(), null) 
        {

        }

        #endregion

        #region Methods

        public async Task<RestRequestResult<T>> GetAsync<T>(string uri, CancellationToken cancellationToken = default) 
            where T: class
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;
            string? content = null;
            TimeSpan elapsed = TimeSpan.Zero;
            Stopwatch stopwatch = new();
            byte[] rawContent;

            restRequest = new RestRequest(uri,
                                          HttpMethod.GET,
                                          null);

            try
            {
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClient.GetAsync(uri, cancellationToken);
                stopwatch.Stop();
            }
            catch(Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                this.Logger?.LogError(exc, "Cannot execute GET REST request.");
                return new RestRequestResult<T>(exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            if (resultHttpMessage != null)
            {
                T? data = default;
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync();

                // Response received.
                if (resultHttpMessage.IsSuccessStatusCode)
                { 
                    // Try decoding data.
                    switch (contentType?.MediaType)
                    {
                        case "application/json":
                            content = this.DecodeString(rawContent, charset);
                            data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    // Maybe it's good to decode the content anyway.
                }

                restRequestResult = new(data,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent);
            }
            else
            {
                HttpClientException httpClientException = new HttpClientException("Http response object is null");
                restRequestResult = new(httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

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

        private string DecodeString(byte[] rawContent, Charset charset)
        {
            string result;

            switch (charset)
            {
                case Charset.UTF8:
                    result = Encoding.UTF8.GetString(rawContent);
                    break;
                case Charset.UTF16:
                    result = Encoding.Unicode.GetString(rawContent);
                    break;
                case Charset.UTF32:
                    result = Encoding.UTF32.GetString(rawContent);
                    break;
                case Charset.ASCII:
                    result = Encoding.ASCII.GetString(rawContent);
                    break;
                case Charset.ISO_8859_1:
                    result = Encoding.GetEncoding("iso-8859-1").GetString(rawContent);
                    break;
                case Charset.WINDOWS_1252:
                    result = Encoding.GetEncoding("windows-1252").GetString(rawContent);
                    break;
                default:
                    result = Encoding.UTF8.GetString(rawContent);
                    break;
            }

            return result;
        }

        #endregion
    }
}
