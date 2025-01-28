using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace AMDevIT.Restling.Core
{
    /// <summary>
    /// Implements a REST client to execute HTTP requests to remote resources.
    /// </summary>
    /// <param name="httpClientContext">A valid <see cref="HttpClientContext"/> that will be used to execute requests</param>
    /// <param name="logger">A valid implementation of <see cref="ILogger"/> that will be used to log REST client messages</param>
    public class RestlingClient(HttpClientContext httpClientContext,
                                ILogger? logger)
        : IRestlingClient, IDisposable
    {
        #region Fields

        private readonly HttpClientContext httpClientContext = httpClientContext;
        private readonly ILogger? logger = logger;
        private bool disposedValue;

        #endregion

        #region Properties

        /// <summary>
        /// Dispose the HttpClient instance when disposing the RestlingClient instance.
        /// </summary>
        public bool DisposeContext
        {
            get;
            set;
        }      

        /// <summary>
        /// Gets a value indicating whether the instance has been disposed.
        /// </summary>
        public bool Disposed => this.disposedValue;

        protected HttpClientContext Context => this.httpClientContext;
        protected ILogger? Logger => this.logger;

        #endregion

        #region .ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient with default values.
        /// </summary>
        public RestlingClient()
            : this(BuildDefaultHttpClientContext(), null) 
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient with default values. 
        /// Use a logger to log the requests and responses of type <see cref="ILogger"/>
        /// </summary>
        /// <param name="logger">The logger instance used to log the messages from the client</param>
        public RestlingClient(ILogger logger)
          : this(BuildDefaultHttpClientContext(), logger)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient build by the <see cref="IHttpClientContextBuilder"/> instance.
        /// </summary>
        /// <param name="httpClientBuilder">The IHttpClientBuilder implementation instance that will be used to build the HttpClient associated to the current client.</param>
        public RestlingClient(IHttpClientContextBuilder httpClientBuilder)
            : this(httpClientBuilder.Build(), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient build by the <see cref="IHttpClientContextBuilder"/> instance.
        /// /// Use a logger to log the requests and responses of type <see cref="ILogger"/>
        /// </summary>
        /// <param name="httpClientBuilder">The IHttpClientBuilder implementation instance that will be used to build the HttpClient associated to the current client.</param>
        /// <param name="logger">The logger instance used to log the messages from the client</param>
        public RestlingClient(IHttpClientContextBuilder httpClientBuilder,
                              ILogger logger)
            : this(httpClientBuilder.Build(), logger)
        {
        }

        #endregion

        #region Methods

        #region GET

        /// <summary>
        /// Execute a GET request to the specified URI and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <param name="uri">The request resource URI</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult> GetAsync(string uri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);

            restRequest = new RestRequest(uri,
                                          HttpMethod.Get,
                                          null);

            try
            {
                this.Logger?.LogDebug("Executing GET REST request to {uri}", uri);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.GetAsync(uri, cancellationToken);
                stopwatch.Stop();
                this.Logger?.LogDebug("GET REST request to {uri} executed in {elapsed} ms", uri, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                this.Logger?.LogError(exc, "Cannot execute GET REST request.");
                return new RestRequestResult(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult> GetAsync(string uri, 
                                                      RequestHeaders requestHeaders, 
                                                      CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Get,
                                          requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync(restRequest, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                                            RequestHeaders requestHeaders,
                                                            CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Get,
                                          requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a GET request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<T>> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);

            restRequest = new RestRequest(uri,
                                          HttpMethod.Get,
                                          null);

            try
            {
                this.Logger?.LogDebug("Executing GET REST request to {uri}", uri);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.GetAsync(uri, cancellationToken);
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

        #endregion

        #region POST

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="D">The request body payload model type</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult> PostAsync<T>(string uri, T requestData, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData);
            try
            {
                HttpContent content = this.BuildHttpContent(requestData);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.PostAsync(uri, content, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute POST REST request.");
                return new RestRequestResult(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="D">The request body payload model type</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri, 
                                                                T requestData, 
                                                                CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<D> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData);
            try
            {
                HttpContent content = this.BuildHttpContent(requestData);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.PostAsync(uri, content, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute POST REST request.");
                return new RestRequestResult<D>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }         

            restRequestResult = await httpResponseParser.DecodeAsync<D>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult> PostAsync<T>(string uri, 
                                                          T requestData,
                                                          RequestHeaders requestHeaders,
                                                          CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData,
                                             requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                                T requestData,
                                                                RequestHeaders requestHeaders,
                                                                CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult<D> restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData,
                                             requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<D,T>(restRequest, cancellationToken);
            return restRequestResult;

        }

        #endregion

        #region PUT

        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData);
            try
            {
                HttpContent content = this.BuildHttpContent(requestData);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.PutAsync(uri, content, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute PUT REST request.");
                return new RestRequestResult(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a PUT request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="D">The request body payload model type</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<D>> PutAsync<D, T>(string uri, 
                                                               T requestData, 
                                                               CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<D> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData);
            try
            {
                HttpContent content = this.BuildHttpContent(requestData);
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.PutAsync(uri, content, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute PUT REST request.");
                return new RestRequestResult<D>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync<D>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         RequestHeaders requestHeaders,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData,
                                             requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                               T requestData,
                                                               RequestHeaders requestHeaders,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult<D> restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData,
                                             requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<D, T>(restRequest, cancellationToken);
            return restRequestResult;
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Execute a DELETE request to the specified URI and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <param name="uri">The request resource URI</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult> DeleteAsync(string uri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest(uri,
                                          HttpMethod.Delete,
                                          null);
            try
            {
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.DeleteAsync(uri, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute DELETE REST request.");
                return new RestRequestResult(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }
            restRequestResult = await httpResponseParser.DecodeAsync(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a DELETE request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? resultHttpMessage = null;
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;
            TimeSpan elapsed;
            Stopwatch stopwatch = new();
            HttpResponseParser httpResponseParser = new(this.Logger);
            restRequest = new RestRequest(uri,
                                          HttpMethod.Delete,
                                          null);
            try
            {
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.DeleteAsync(uri, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();
                this.Logger?.LogError(exc, "Cannot execute DELETE REST request.");
                return new RestRequestResult<T>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }
            restRequestResult = await httpResponseParser.DecodeAsync<T>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult> DeleteAsync(string uri,
                                                         RequestHeaders requestHeaders,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Post,
                                          requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync(restRequest, cancellationToken);
            return restRequestResult;
        }

        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                               RequestHeaders requestHeaders,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Post,
                                          requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken);
            return restRequestResult;
        }

        #endregion

        /// <summary>
        /// Execute a REST request and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <param name="restRequest">A <see cref="RestRequest"/> parameter that indicates the request method and uri.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        /// <exception cref="ArgumentException">If custom method mode is specified, but the custom method verb name is not set, 
        /// an argument exception will be throw.</exception>
        /// <exception cref="NotSupportedException">If a http method different from Get, Post, Put, Delete, Head, 
        /// Options, Trace, Patch or Custom a NotSupportedException will be throw.</exception>
        public async Task<RestRequestResult> ExecuteRequestAsync(RestRequest restRequest, 
                                                                 CancellationToken cancellationToken = default)
        {
            HttpRequestMessage httpRequest;
            RestRequestResult restRequestResult;

            ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
            ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

            httpRequest = this.BuildHttpRequestMessage(restRequest);                   
            restRequestResult = await this.ExecuteRequestInternalAsync(restRequest, httpRequest, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a REST request and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="restRequest">A <see cref="RestRequest"/> parameter that indicates the request method and uri.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        /// <exception cref="ArgumentException">If custom method mode is specified, but the custom method verb name is not set, 
        /// an argument exception will be throw.</exception>
        /// <exception cref="NotSupportedException">If a http method different from Get, Post, Put, Delete, Head, 
        /// Options, Trace, Patch or Custom a NotSupportedException will be throw.</exception>
        public async Task<RestRequestResult<T>> ExecuteRequestAsync<T>(RestRequest restRequest,
                                                                       CancellationToken cancellationToken = default)
        {   
            HttpRequestMessage httpRequest;
            RestRequestResult<T> restRequestResult;

            ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
            ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

            httpRequest = this.BuildHttpRequestMessage(restRequest);           
            restRequestResult = await this.ExecuteRequestInternalAsync<T>(restRequest, httpRequest, cancellationToken);
            return restRequestResult;
        }

        /// <summary>
        /// Execute a REST request and return the result as a <see cref="RestRequestResult"/> instance. 
        /// </summary>
        /// <typeparam name="D">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="restRequest">A <see cref="RestRequest"/> parameter that indicates the request method and uri.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        /// <exception cref="ArgumentException">If custom method mode is specified, but the custom method verb name is not set, 
        /// an argument exception will be throw.</exception>
        /// <exception cref="NotSupportedException">If a http method different from Get, Post, Put, Delete, Head, 
        /// Options, Trace, Patch or Custom a NotSupportedException will be throw.</exception>
        public async Task<RestRequestResult<D>> ExecuteRequestAsync<D, T>(RestRequest<T> restRequest,
                                                                          CancellationToken cancellationToken = default)
        {
            HttpRequestMessage httpRequest;
            RestRequestResult<D> restRequestResult;

            ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
            ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

            httpRequest = this.BuildHttpRequestMessage<T>(restRequest);
            restRequestResult = await this.ExecuteRequestInternalAsync<D>(restRequest, httpRequest, cancellationToken);
            return restRequestResult;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected async Task<RestRequestResult> ExecuteRequestInternalAsync(RestRequest restRequest,
                                                                            HttpRequestMessage httpRequest,
                                                                            CancellationToken cancellationToken = default)
        {
            HttpResponseParser httpResponseParser = new(this.Logger);
            RestRequestResult restRequestResult;
            HttpResponseMessage? resultHttpMessage = null;
            Stopwatch stopwatch = new();
            TimeSpan elapsed;

            try
            {
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.SendAsync(httpRequest, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                this.Logger?.LogError(exc, "Cannot execute {method} REST request.", httpRequest.Method.Method);
                return new RestRequestResult(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        protected async Task<RestRequestResult<T>> ExecuteRequestInternalAsync<T>(RestRequest restRequest, 
                                                                                  HttpRequestMessage httpRequest, 
                                                                                  CancellationToken cancellationToken = default)
        {
            HttpResponseParser httpResponseParser = new(this.Logger);
            RestRequestResult<T> restRequestResult;
            HttpResponseMessage? resultHttpMessage = null;
            Stopwatch stopwatch = new();
            TimeSpan elapsed;

            try
            {
                stopwatch = Stopwatch.StartNew();
                resultHttpMessage = await this.httpClientContext.HttpClient.SendAsync(httpRequest, cancellationToken);
                stopwatch.Stop();
            }
            catch (Exception exc)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                this.Logger?.LogError(exc, "Cannot execute {method} REST request.", httpRequest.Method.Method);
                return new RestRequestResult<T>(restRequest, exc, stopwatch.Elapsed);
            }
            finally
            {
                elapsed = stopwatch.Elapsed;
            }

            restRequestResult = await httpResponseParser.DecodeAsync<T>(resultHttpMessage, restRequest, elapsed, cancellationToken);
            return restRequestResult;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.DisposeContext)
                    {
                        this.httpClientContext.Dispose();
                    }
                }
                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose the HttpClient instance and all the handlers when disposing the RestlingClient instance, if <see cref="DisposeContext"/> is set to true.
        /// </summary>


        protected HttpContent BuildHttpContent<T>(T requestData)
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

        protected HttpRequestMessage BuildHttpRequestMessage(RestRequest restRequest)
        {
            HttpRequestMessage httpRequest;
            NetHttpMethod requestHttpMethod;

            requestHttpMethod = restRequest.Method switch
            {
                HttpMethod.Get => NetHttpMethod.Get,
                HttpMethod.Post => NetHttpMethod.Post,
                HttpMethod.Put => NetHttpMethod.Put,
                HttpMethod.Delete => NetHttpMethod.Delete,
                HttpMethod.Head => NetHttpMethod.Head,
                HttpMethod.Options => NetHttpMethod.Options,
                HttpMethod.Trace => NetHttpMethod.Trace,
                HttpMethod.Patch => NetHttpMethod.Patch,
                HttpMethod.Custom => new NetHttpMethod(restRequest.CustomMethod ??
                                                       throw new ArgumentException("Custom method cannot be null")),
                _ => throw new NotSupportedException($"The HTTP method {restRequest.Method} is not supported.")
            };

            httpRequest = new HttpRequestMessage(requestHttpMethod, restRequest.Uri);

            if (restRequest.Headers.AuthenticationHeader != null)
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(restRequest.Headers.AuthenticationHeader.Scheme,
                                                                                  restRequest.Headers.AuthenticationHeader.Parameter);

            if (restRequest.Headers.Headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> header in restRequest.Headers.Headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }

            return httpRequest;
        }

        protected HttpRequestMessage BuildHttpRequestMessage<T>(RestRequest<T> restRequest)
        {
            HttpRequestMessage httpRequest;
            NetHttpMethod requestHttpMethod;

            requestHttpMethod = restRequest.Method switch
            {
                HttpMethod.Get => NetHttpMethod.Get,
                HttpMethod.Post => NetHttpMethod.Post,
                HttpMethod.Put => NetHttpMethod.Put,
                HttpMethod.Delete => NetHttpMethod.Delete,
                HttpMethod.Head => NetHttpMethod.Head,
                HttpMethod.Options => NetHttpMethod.Options,
                HttpMethod.Trace => NetHttpMethod.Trace,
                HttpMethod.Patch => NetHttpMethod.Patch,
                HttpMethod.Custom => new NetHttpMethod(restRequest.CustomMethod ??
                                                       throw new ArgumentException("Custom method cannot be null")),
                _ => throw new NotSupportedException($"The HTTP method {restRequest.Method} is not supported.")
            };

            httpRequest = new HttpRequestMessage(requestHttpMethod, restRequest.Uri);

            if (restRequest.Headers.AuthenticationHeader != null)
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(restRequest.Headers.AuthenticationHeader.Scheme,
                                                                                  restRequest.Headers.AuthenticationHeader.Parameter);

            if (restRequest.Headers.Headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> header in restRequest.Headers.Headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }

            if (restRequest.RequestData != null)
                httpRequest.Content = this.BuildHttpContent<T>(restRequest.RequestData);

            return httpRequest;
        }

        protected static HttpClientContext BuildDefaultHttpClientContext()
        {
            HttpClientContextBuilder httpClientBuilder = new();
            return httpClientBuilder.Build();
        }

        #endregion
    }
}
