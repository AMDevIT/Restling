using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Network.Pipeline;
using AMDevIT.Restling.Core.Codecs;
using System.Net.Http.Headers;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Core.Multipart;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Runtime.CompilerServices;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace AMDevIT.Restling.Core
{
    /// <summary>
    /// Implements a REST client to execute HTTP requests to remote resources.
    /// </summary>
    public class RestlingClient : IRestlingClient, IDisposable
    {
        #region Fields

        private readonly HttpClientContext httpClientContext;
        private readonly HttpExecutionPipeline httpExecutionPipeline;
        private readonly ILogger? logger;
        private RestlingClientContextOwnership contextOwnership;
        private bool disposedValue;

        #endregion

        #region Properties

        public HttpClientContext ClientContext => this.httpClientContext;

        /// <summary>
        /// Allow to force the serialization library to use when serializing the payload data.
        /// </summary>
        /// <remarks>Default to aumetic, that uses reflection to check for attributes 
        /// from System.Text.Json or NewtonSoft JSON into the class or interface.</remarks>
        public PayloadJsonSerializerLibrary SelectedDefaultSerializationLibrary
        {
            get;
            set;
        } = PayloadJsonSerializerLibrary.Automatic;

        /// <summary>Gets or sets whether this client owns and disposes its context.</summary>
        public RestlingClientContextOwnership ContextOwnership
        {
            get => this.contextOwnership;
            set
            {
                if (!Enum.IsDefined(value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                this.contextOwnership = value;
            }
        }

        /// <summary>Compatibility alias for ContextOwnership.</summary>
        public bool DisposeContext
        {
            get => this.ContextOwnership == RestlingClientContextOwnership.Owned;
            set => this.ContextOwnership = value
                ? RestlingClientContextOwnership.Owned
                : RestlingClientContextOwnership.Borrowed;
        }

        /// <summary>
        /// Gets a value indicating whether the instance has been disposed.
        /// </summary>
        public bool Disposed => this.disposedValue;

        public bool EnableVerboseLogging
        {
            get;
            set;
        } = true;

        protected HttpClientContext Context => this.httpClientContext;
        protected ILogger? Logger => this.logger;

        #endregion

        #region .ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient with default values.
        /// </summary>
        public RestlingClient()
            : this(BuildDefaultHttpClientContext(), null, RestlingClientContextOwnership.Owned)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using a dedicated HttpClient with default values. 
        /// Use a logger to log the requests and responses of type <see cref="ILogger"/>
        /// </summary>
        /// <param name="logger">The logger instance used to log the messages from the client</param>
        public RestlingClient(ILogger logger)
            : this(BuildDefaultHttpClientContext(), logger, RestlingClientContextOwnership.Owned)
        {

        }

        /// <summary>
        /// Initializes a new client that borrows an externally managed context.
        /// </summary>
        /// <param name="httpClientContext">The context that remains owned by the caller.</param>
        /// <param name="logger">The optional logger used by this client.</param>
        public RestlingClient(HttpClientContext httpClientContext, ILogger? logger)
            : this(httpClientContext, logger, RestlingClientContextOwnership.Borrowed)
        {
        }

        /// <summary>Initializes a client with an explicit context ownership contract.</summary>
        /// <param name="httpClientContext">The context used by the client.</param>
        /// <param name="logger">The optional logger used by this client.</param>
        /// <param name="contextOwnership">Whether the client borrows or owns the context.</param>
        public RestlingClient(HttpClientContext httpClientContext,
                              ILogger? logger,
                              RestlingClientContextOwnership contextOwnership)
        {
            ArgumentNullException.ThrowIfNull(httpClientContext);
            this.httpClientContext = httpClientContext;
            this.logger = logger;
            this.httpExecutionPipeline = new(httpClientContext.ResolveHttpClient,
                                              httpClientContext.InvalidateHttpClient,
                                              httpClientContext.EnsureCookieStorageLoadedAsync,
                                              httpClientContext.NotifyCookiesChanged,
                                              httpClientContext.Codecs,
                                              logger);
            this.ContextOwnership = contextOwnership;
        }

        /// <summary>Initializes a new client that borrows an externally managed context.</summary>
        /// <param name="httpClientContext">The context that remains owned by the caller.</param>
        public RestlingClient(HttpClientContext httpClientContext)
            : this(httpClientContext, null, RestlingClientContextOwnership.Borrowed)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestlingClient"/> class using an owned context built by the supplied builder.
        /// </summary>
        /// <param name="httpClientBuilder">The builder used to create the context owned by this client.</param>
        public RestlingClient(IHttpClientContextBuilder httpClientBuilder)
            : this(BuildContext(httpClientBuilder), null, RestlingClientContextOwnership.Owned)
        {
        }

        /// <summary>Initializes a new client with an explicit context ownership contract.</summary>
        /// <param name="httpClientContext">The context used by the client.</param>
        /// <param name="contextOwnership">Whether the client borrows or owns the context.</param>
        public RestlingClient(HttpClientContext httpClientContext,
                              RestlingClientContextOwnership contextOwnership)
            : this(httpClientContext, null, contextOwnership)
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
            : this(BuildContext(httpClientBuilder), logger, RestlingClientContextOwnership.Owned)
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
            RestRequest restRequest;

            restRequest = new RestRequest(uri, HttpMethod.Get);
            return await this.httpExecutionPipeline.ExecuteAsync(restRequest,
                                                                 () => this.BuildDirectHttpRequestMessage(restRequest),
                                                                 cancellationToken);
        }

        /// <summary>Executes a GET request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult> GetAsync(string uri,
                                                      RequestProxyOptions proxyOptions,
                                                      CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Get) { ProxyOptions = proxyOptions };
            return await this.httpExecutionPipeline.ExecuteAsync(restRequest,
                                                                 () => this.BuildDirectHttpRequestMessage(restRequest),
                                                                 cancellationToken);
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

            restRequestResult = await this.ExecuteRequestAsync(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a GET request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult> GetAsync(string uri,
                                                      RequestHeaders requestHeaders,
                                                      RequestProxyOptions proxyOptions,
                                                      CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Get, requestHeaders) { ProxyOptions = proxyOptions };
            return await this.ExecuteRequestAsync(restRequest, cancellationToken: cancellationToken);
        }

        public async Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                                            RequestHeaders requestHeaders,
                                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                            CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Get,
                                          requestHeaders);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a typed GET request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                                            RequestHeaders requestHeaders,
                                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                            RequestProxyOptions proxyOptions,
                                                            CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Get, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteRequestAsync<T>(restRequest, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Execute a GET request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="forcePayloadJsonSerializerLibrary">Force the json libary used to serialize and deserialize content. Default to null, equivalent to automatic.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<T>> GetAsync<T>(string uri, 
                                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null, 
                                                            CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;

            restRequest = new RestRequest(uri, HttpMethod.Get);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteTypedRequestAsync<T>(restRequest,
                                                          restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                          cancellationToken);
        }

        /// <summary>Executes a typed GET request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                            RequestProxyOptions proxyOptions,
                                                            CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Get) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteTypedRequestAsync<T>(restRequest,
                                                          restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                          cancellationToken);
        }

#endregion

        #region POST

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="D">The request body payload model type</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData">The request data payload</param>
        /// <param name="forcePayloadJsonSerializerLibrary">Force the json libary used to serialize and deserialize content. Default to null, equivalent to automatic.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult> PostAsync<T>(string uri, 
                                                          T requestData,
                                                          PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                          CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync(restRequest, cancellationToken);
        }

        /// <summary>Executes a POST request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult> PostAsync<T>(string uri,
                                                          T requestData,
                                                          PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                          RequestProxyOptions proxyOptions,
                                                          CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Post, requestData) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync(restRequest, cancellationToken);
        }

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="D">The request body payload model type</typeparam>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData">The request data payload</param>
        /// <param name="forcePayloadJsonSerializerLibrary">Force the json libary used to serialize and deserialize content. Default to null, equivalent to automatic.</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri, 
                                                                T requestData,
                                                                PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                                CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync<D, T>(restRequest,
                                                               restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                               cancellationToken);
        }

        /// <summary>Executes a typed POST request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                                T requestData,
                                                                PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                                RequestProxyOptions proxyOptions,
                                                                CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Post, requestData) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync<D, T>(restRequest,
                                                               restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                               cancellationToken);
        }

        public async Task<RestRequestResult> PostAsync<T>(string uri, 
                                                          T requestData,
                                                          RequestHeaders requestHeaders,
                                                          PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                          CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData,
                                             requestHeaders);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteHeaderPayloadRequestAsync(restRequest, cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a POST request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult> PostAsync<T>(string uri,
                                                          T requestData,
                                                          RequestHeaders requestHeaders,
                                                          PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                          RequestProxyOptions proxyOptions,
                                                          CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Post, requestData, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteHeaderPayloadRequestAsync(restRequest, cancellationToken);
        }

        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                                T requestData,
                                                                RequestHeaders requestHeaders,
                                                                PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                                CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult<D> restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Post,
                                             requestData,
                                             requestHeaders);
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteRequestAsync<D,T>(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;

        }

        /// <summary>Executes a typed POST request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                                T requestData,
                                                                RequestHeaders requestHeaders,
                                                                PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                                RequestProxyOptions proxyOptions,
                                                                CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Post, requestData, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteRequestAsync<D, T>(restRequest, cancellationToken: cancellationToken);
        }

        #endregion

        #region PUT

        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync(restRequest, cancellationToken);
        }

        /// <summary>Executes a PUT request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                         RequestProxyOptions proxyOptions,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Put, requestData) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync(restRequest, cancellationToken);
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
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync<D, T>(restRequest,
                                                               restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                               cancellationToken);
        }

        /// <summary>Executes a typed PUT request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                               T requestData,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                               RequestProxyOptions proxyOptions,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Put, requestData) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecutePayloadRequestAsync<D, T>(restRequest,
                                                               restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                               cancellationToken);
        }

        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         RequestHeaders requestHeaders,
                                                         PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData,
                                             requestHeaders);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteHeaderPayloadRequestAsync(restRequest, cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a PUT request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult> PutAsync<T>(string uri,
                                                         T requestData,
                                                         RequestHeaders requestHeaders,
                                                         PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                         RequestProxyOptions proxyOptions,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Put, requestData, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteHeaderPayloadRequestAsync(restRequest, cancellationToken);
        }

        public async Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                               T requestData,
                                                               RequestHeaders requestHeaders,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest;
            RestRequestResult<D> restRequestResult;

            restRequest = new RestRequest<T>(uri,
                                             HttpMethod.Put,
                                             requestData,
                                             requestHeaders);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteRequestAsync<D, T>(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a typed PUT request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                               T requestData,
                                                               RequestHeaders requestHeaders,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                               RequestProxyOptions proxyOptions,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest<T> restRequest = new(uri, HttpMethod.Put, requestData, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteRequestAsync<D, T>(restRequest, cancellationToken: cancellationToken);
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
            RestRequest restRequest;

            restRequest = new RestRequest(uri, HttpMethod.Delete);
            return await this.httpExecutionPipeline.ExecuteAsync(restRequest,
                                                                 () => this.BuildDirectHttpRequestMessage(restRequest),
                                                                 cancellationToken);
        }

        /// <summary>Executes a DELETE request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult> DeleteAsync(string uri,
                                                         RequestProxyOptions proxyOptions,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Delete) { ProxyOptions = proxyOptions };
            return await this.httpExecutionPipeline.ExecuteAsync(restRequest,
                                                                 () => this.BuildDirectHttpRequestMessage(restRequest),
                                                                 cancellationToken);
        }

        /// <summary>
        /// Execute a DELETE request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;

            restRequest = new RestRequest(uri, HttpMethod.Delete);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteTypedRequestAsync<T>(restRequest,
                                                          restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                          cancellationToken);
        }

        /// <summary>Executes a typed DELETE request with a per-request proxy selection.</summary>
        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                               RequestProxyOptions proxyOptions,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Delete) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteTypedRequestAsync<T>(restRequest,
                                                          restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                                                          cancellationToken);
        }

        public async Task<RestRequestResult> DeleteAsync(string uri,
                                                         RequestHeaders requestHeaders,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Delete,
                                          requestHeaders);

            restRequestResult = await this.ExecuteRequestAsync(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a DELETE request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult> DeleteAsync(string uri,
                                                         RequestHeaders requestHeaders,
                                                         RequestProxyOptions proxyOptions,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Delete, requestHeaders) { ProxyOptions = proxyOptions };
            return await this.ExecuteRequestAsync(restRequest, cancellationToken: cancellationToken);
        }

        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                               RequestHeaders requestHeaders,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest restRequest;
            RestRequestResult<T> restRequestResult;

            restRequest = new RestRequest(uri,
                                          HttpMethod.Delete,
                                          requestHeaders);

            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;

            restRequestResult = await this.ExecuteRequestAsync<T>(restRequest, cancellationToken: cancellationToken);
            return restRequestResult;
        }

        /// <summary>Executes a typed DELETE request with headers and a per-request proxy selection.</summary>
        public async Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                               RequestHeaders requestHeaders,
                                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary,
                                                               RequestProxyOptions proxyOptions,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequest restRequest = new(uri, HttpMethod.Delete, requestHeaders) { ProxyOptions = proxyOptions };
            if (forcePayloadJsonSerializerLibrary != null)
                restRequest.ForcePayloadJsonSerializerLibrary = forcePayloadJsonSerializerLibrary;
            return await this.ExecuteRequestAsync<T>(restRequest, cancellationToken: cancellationToken);
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
                                                                 bool throwOnGenerics = true,
                                                                 CancellationToken cancellationToken = default)
        {
            RestRequestResult restRequestResult;

            switch (restRequest)
            {
                case MultipartRequest multipartRequest:
                    {
                        restRequestResult = await this.ExecuteMultipartRequestAsync(multipartRequest, cancellationToken);
                    }
                    break;

                case FormUrlEncodedRequest formUrlEncodedRequest:
                    {
                        restRequestResult = await this.ExecuteFormUrlEncodedRequest(formUrlEncodedRequest, cancellationToken);
                    }
                    break;

                case RestRawRequest restRawRequest:
                    {
                        restRequestResult = await this.ExecuteRawRequestAsync(restRawRequest, cancellationToken);
                    }
                    break;

                default:
                case RestRequest _:
                    {
                        HttpRequestMessage httpRequest;

                        ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
                        ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

                        using (httpRequest = BuildHttpRequestMessage(restRequest))
                        {
                            restRequestResult = await this.ExecuteRequestInternalAsync(restRequest,
                                                                                       httpRequest,
                                                                                       throwOnGenerics: throwOnGenerics,
                                                                                       cancellationToken: cancellationToken);
                        }
                        break;
                    }
            }

            //HttpRequestMessage httpRequest;
            

            //ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
            //ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

            //httpRequest = BuildHttpRequestMessage(restRequest);                   
            //restRequestResult = await this.ExecuteRequestInternalAsync(restRequest, 
            //                                                           httpRequest, 
            //                                                           throwOnGenerics: throwOnGenerics, 
            //                                                           cancellationToken: cancellationToken);

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
                                                                       bool throwOnGenerics = false,
                                                                       CancellationToken cancellationToken = default)
        {
            RestRequestResult<T> restRequestResult;

            switch (restRequest)
            {
                case MultipartRequest multipartRequest:
                    {
                        restRequestResult = await this.ExecuteMultipartRequestAsync<T>(multipartRequest, cancellationToken);
                    }
                    break;

                case FormUrlEncodedRequest formUrlEncodedRequest:
                    {
                        restRequestResult = await this.ExecuteFormUrlEncodedRequest<T>(formUrlEncodedRequest, cancellationToken);
                    }
                    break;

                case RestRawRequest restRawRequest:
                    {
                        restRequestResult = await this.ExecuteRawRequestAsync<T>(restRawRequest, 
                                                                                 cancellationToken);
                    }
                    break;

                default:
                case RestRequest _:
                    {
                        HttpRequestMessage httpRequest;

                        ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
                        ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

                        //if (restRequest.GetType().IsGenericType == true)
                        //    throw new InvalidOperationException("The rest request contains generic type data. Cannot be executed with using a call without payload.");

                        using (httpRequest = this.BuildHttpRequestMessage(restRequest))
                        {
                            restRequestResult = await this.ExecuteRequestInternalAsync<T>(restRequest,
                                                                                          httpRequest,
                                                                                          throwOnGenerics: throwOnGenerics,
                                                                                          cancellationToken);
                        }
                    }
                    break;
            }
            
            return restRequestResult;
        }

        /// <summary>Executes a multipart request.</summary>
        public async Task<RestRequestResult> ExecuteMultipartRequestAsync(MultipartRequest multipartRequest,
                                                                          CancellationToken cancellationToken = default)
        {
            HttpRequestMessage httpRequest;
            RestRequestResult result;

            ArgumentNullException.ThrowIfNull(multipartRequest);
            using (httpRequest = this.BuildHttpRequestMessage(multipartRequest))
            {
                httpRequest.Content = this.BuildMultipartHttpContent(multipartRequest);
                result = await this.ExecuteRequestInternalAsync(multipartRequest,
                                                                httpRequest,
                                                                cancellationToken: cancellationToken);
            }
            return result;
        }

        /// <summary>Executes a multipart request and deserializes its response.</summary>
        public async Task<RestRequestResult<T>> ExecuteMultipartRequestAsync<T>(MultipartRequest multipartRequest,
                                                                                CancellationToken cancellationToken = default)
        {
            HttpRequestMessage httpRequest;
            RestRequestResult<T> result;

            ArgumentNullException.ThrowIfNull(multipartRequest);
            using (httpRequest = this.BuildHttpRequestMessage(multipartRequest))
            {
                httpRequest.Content = this.BuildMultipartHttpContent(multipartRequest);
                result = await this.ExecuteRequestInternalAsync<T>(multipartRequest,
                                                                   httpRequest,
                                                                   cancellationToken: cancellationToken);
            }
            return result;
        }

        /// <summary>Streams parts from a multipart/x-mixed-replace response until cancellation or its closing boundary.</summary>
        public async IAsyncEnumerable<MultipartPart> StreamMultipartMixedReplaceAsync(RestRequest restRequest,
                                                                                      MultipartOptions? options = null,
                                                                                      [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ContentCodecContext codecContext;
            HttpRequestMessage httpRequest;
            MediaTypeHeaderValue contentType;

            ArgumentNullException.ThrowIfNull(restRequest);
            options ??= new MultipartOptions();
            options.Validate();

            using (httpRequest = this.BuildHttpRequestMessage(restRequest))
            using (HttpResponseLease lease = await this.httpExecutionPipeline.SendStreamingAsync(restRequest,
                                                                                                  httpRequest,
                                                                                                  cancellationToken))
            {
                HttpResponseMessage response = lease.Response;
                response.EnsureSuccessStatusCode();
                contentType = response.Content.Headers.ContentType ??
                              throw new InvalidDataException("The multipart response has no Content-Type header.");
                if (!string.Equals(contentType.MediaType, "multipart/x-mixed-replace", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The response is not multipart/x-mixed-replace.");

                codecContext = new ContentCodecContext
                {
                    Logger = this.Logger,
                    JsonSerializerLibrary = restRequest.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                    Codecs = this.Context.Codecs
                };

                using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await foreach (MultipartPart part in MultipartMixedReplaceReader.ReadAsync(stream,
                                                                                            contentType,
                                                                                            this.Context.Codecs,
                                                                                            codecContext,
                                                                                            options,
                                                                                            cancellationToken))
                {
                    yield return part;
                }
            }
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
                                                                          bool throwOnGenerics = false,
                                                                          CancellationToken cancellationToken = default)
        {
            HttpRequestMessage httpRequest;
            RestRequestResult<D> restRequestResult;

            ArgumentNullException.ThrowIfNull(restRequest, nameof(restRequest));
            ArgumentException.ThrowIfNullOrWhiteSpace(restRequest.Uri, nameof(restRequest.Uri));

            using (httpRequest = this.BuildHttpRequestMessageWithPayload<T>(restRequest))
            {
                restRequestResult = await this.ExecuteRequestInternalAsync<D>(restRequest,
                                                                              httpRequest,
                                                                              throwOnGenerics: throwOnGenerics,
                                                                              cancellationToken: cancellationToken);
            }
            return restRequestResult;
        }

        public async Task<RestRequestResult> ExecuteFormUrlEncodedRequest(FormUrlEncodedRequest formUrlEncodedRequest, 
                                                                          CancellationToken cancellationToken = default)
        {
            RestRequestResult restRequestResult;
            HttpRequestMessage httpRequest;

            using (httpRequest = this.BuildHttpRequestMessage(formUrlEncodedRequest))
            {
                httpRequest.Content = this.BuildFormUrlEncodedContent(formUrlEncodedRequest.Parameters);
                restRequestResult = await this.httpExecutionPipeline.ExecuteAsync(formUrlEncodedRequest,
                                                                                  httpRequest,
                                                                                  cancellationToken);
            }

            return restRequestResult;
        }

        public async Task<RestRequestResult<T>> ExecuteFormUrlEncodedRequest<T>(FormUrlEncodedRequest formUrlEncodedRequest,
                                                                                CancellationToken cancellationToken = default)
        {
            RestRequestResult<T> restRequestResult;
            HttpRequestMessage httpRequest;

            using (httpRequest = this.BuildHttpRequestMessage(formUrlEncodedRequest))
            {
                httpRequest.Content = this.BuildFormUrlEncodedContent(formUrlEncodedRequest.Parameters);
                restRequestResult = await this.httpExecutionPipeline.ExecuteAsync<T>(formUrlEncodedRequest,
                                                                                     httpRequest,
                                                                                     formUrlEncodedRequest.ForcePayloadJsonSerializerLibrary,
                                                                                     cancellationToken);
            }

            return restRequestResult;
        }

        public async Task<RestRequestResult> ExecuteRawRequestAsync(RestRawRequest restRawRequest,
                                                                    CancellationToken cancellationToken = default)
        {
            RestRequestResult restRequestResult;
            HttpRequestMessage httpRequest;

            using (httpRequest = this.BuildHttpRequestMessage(restRawRequest))
            {
                httpRequest.Content = this.BuildRawHttpContent(restRawRequest.Content,
                                                               restRawRequest.ContentType);
                restRequestResult = await this.httpExecutionPipeline.ExecuteAsync(restRawRequest,
                                                                                  httpRequest,
                                                                                  cancellationToken);
            }

            return restRequestResult;
        }

        public async Task<RestRequestResult<T>> ExecuteRawRequestAsync<T>(RestRawRequest restRawRequest,
                                                                          CancellationToken cancellationToken = default)
        {
            RestRequestResult<T> restRequestResult;
            HttpRequestMessage httpRequest;

            using (httpRequest = this.BuildHttpRequestMessage(restRawRequest))
            {
                httpRequest.Content = this.BuildRawHttpContent(restRawRequest.Content,
                                                               restRawRequest.ContentType);
                restRequestResult = await this.httpExecutionPipeline.ExecuteAsync<T>(restRawRequest,
                                                                                     httpRequest,
                                                                                     restRawRequest.ForcePayloadJsonSerializerLibrary,
                                                                                     cancellationToken);
            }

            return restRequestResult;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected async Task<RestRequestResult> ExecuteRequestInternalAsync(RestRequest restRequest,
                                                                            HttpRequestMessage httpRequest,
                                                                            bool throwOnGenerics = false,
                                                                            CancellationToken cancellationToken = default)
        {
            if (throwOnGenerics && restRequest.GetType().IsGenericType)
                throw new InvalidOperationException("The rest request contains generic type data. Cannot be executed with using a call without payload.");

            restRequest.ForcePayloadJsonSerializerLibrary = this.SelectedDefaultSerializationLibrary switch
            {
                PayloadJsonSerializerLibrary.Automatic => restRequest.ForcePayloadJsonSerializerLibrary,
                _ => this.SelectedDefaultSerializationLibrary
            };
            return await this.httpExecutionPipeline.ExecuteAsync(restRequest, httpRequest, cancellationToken);
        }

        protected async Task<RestRequestResult<T>> ExecuteRequestInternalAsync<T>(RestRequest restRequest, 
                                                                                  HttpRequestMessage httpRequest,
                                                                                  bool throwOnGenerics = false,
                                                                                  CancellationToken cancellationToken = default)
        {
            if (throwOnGenerics && restRequest.GetType().IsGenericType)
                throw new InvalidOperationException("The rest request contains generic type data. Cannot be executed with using a call without payload.");

            restRequest.ForcePayloadJsonSerializerLibrary = this.SelectedDefaultSerializationLibrary switch
            {
                PayloadJsonSerializerLibrary.Automatic => restRequest.ForcePayloadJsonSerializerLibrary,
                _ => this.SelectedDefaultSerializationLibrary
            };
            return await this.httpExecutionPipeline.ExecuteAsync<T>(restRequest,
                                                                    httpRequest,
                                                                    restRequest.ForcePayloadJsonSerializerLibrary,
                                                                    cancellationToken);
        }

        /// <summary>
        /// Disposes the context only when ContextOwnership is Owned.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.ContextOwnership == RestlingClientContextOwnership.Owned)
                    {
                        this.httpClientContext.Dispose();
                    }
                }
                this.disposedValue = true;
            }
        }       

        protected HttpContent BuildRawHttpContent(string? contentBody = null, 
                                                  string? requestContentMediaType = null)
        {
            HttpContent httpContent;

            if (this.EnableVerboseLogging)
                this.Logger?.LogTrace("Building raw http content.");

            if (contentBody == null)
                contentBody = string.Empty;

            httpContent = new StringContent(contentBody, Encoding.UTF8, requestContentMediaType ?? HttpMediaType.TextPlain);
            return httpContent;
        }

        protected HttpContent BuildFormUrlEncodedContent(IDictionary<string, string> content)
        {
            HttpContent httpContent;

            if (this.EnableVerboseLogging)
                this.Logger?.LogTrace("Building form url encoded http content.");

            httpContent = new FormUrlEncodedContent(content);
            return httpContent;
        }

        /// <summary>Builds multipart content using the current codec snapshot.</summary>
        protected System.Net.Http.MultipartContent BuildMultipartHttpContent(MultipartRequest request)
        {
            ContentCodecContext context = new()
            {
                Logger = this.Logger,
                JsonSerializerLibrary = request.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                Codecs = this.Context.Codecs
            };
            return request.BuildContent(this.Context.Codecs, context);
        }

        /// <summary>Builds JSON content while preserving legacy media-type labels and null payloads.</summary>
        protected HttpContent BuildJsonHttpContent<T>(T requestData,
                                                      string? requestContentMediaType = null,
                                                      PayloadJsonSerializerLibrary? payloadJsonSerializerLibrary = null)
        {
            if (requestData == null)
                return new StringContent(string.Empty);

            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(requestContentMediaType)
                ? HttpMediaType.ApplicationJson
                : requestContentMediaType);
            IContentCodec codec = this.Context.Codecs.FindWriter(HttpMediaType.ApplicationJson)
                ?? throw new NotSupportedException("No JSON writer is registered.");
            ContentCodecContext context = new()
            {
                Logger = this.Logger,
                JsonSerializerLibrary = payloadJsonSerializerLibrary,
                Codecs = this.Context.Codecs
            };
            return codec.Serialize(requestData, contentType, context);
        }

        /// <summary>Builds a payload with the codec explicitly selected by its media type.</summary>
        protected HttpContent BuildCodecHttpContent<T>(RestRequest<T> request)
        {
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse(request.ContentMediaType ?? HttpMediaType.ApplicationJson);
            IContentCodec codec = this.Context.Codecs.FindWriter(contentType.MediaType)
                ?? throw new NotSupportedException($"No writer is registered for {contentType.MediaType}.");
            ContentCodecContext context = new()
            {
                Logger = this.Logger,
                JsonSerializerLibrary = request.ForcePayloadJsonSerializerLibrary ?? this.SelectedDefaultSerializationLibrary,
                Codecs = this.Context.Codecs
            };
            return codec.Serialize(request.RequestData, contentType, context);
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

            if (this.EnableVerboseLogging)
                this.Logger?.LogTrace("Building request {httpRequestVerb} for uri {requestUri}", requestHttpMethod, restRequest.Uri);

            httpRequest = new HttpRequestMessage(requestHttpMethod, restRequest.Uri);

            if (restRequest.Headers.AuthenticationHeader != null)
            {                
                httpRequest.Headers.Authorization = new (restRequest.Headers.AuthenticationHeader.Scheme,
                                                         restRequest.Headers.AuthenticationHeader.Parameter);

                if (this.EnableVerboseLogging)
                    this.Logger?.LogTrace("Authentication header set to {authHeader}", httpRequest.Headers.Authorization.ToString());
            }            

            if (restRequest.Headers.Headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> header in restRequest.Headers.Headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                    if (this.EnableVerboseLogging)
                        this.Logger?.LogTrace("Added header {headerKey} with value {headerValue}", header.Key, header.Value);
                }
            }

            return httpRequest;
        }

        protected HttpRequestMessage BuildHttpRequestMessageWithPayload<T>(RestRequest<T> restRequest)
        {
            HttpRequestMessage httpRequest;         

            httpRequest = this.BuildHttpRequestMessage(restRequest);

            if (restRequest.UseContentCodec)
            {
                httpRequest.Content = this.BuildCodecHttpContent(restRequest);
            }
            else if (restRequest.RequestData != null)
            {
                httpRequest.Content = this.BuildJsonHttpContent<T>(restRequest.RequestData, 
                                                               requestContentMediaType: restRequest.ContentMediaType,
                                                               payloadJsonSerializerLibrary: restRequest.ForcePayloadJsonSerializerLibrary);
            }

            return httpRequest;
        }

        protected static HttpClientContext BuildDefaultHttpClientContext()
        {
            HttpClientContextBuilder httpClientBuilder = new();
            return httpClientBuilder.Build();
        }

        /// <summary>Sends a payload with per-request headers and returns an untyped response.</summary>
        private async Task<RestRequestResult> ExecuteHeaderPayloadRequestAsync<T>(RestRequest<T> restRequest,
                                                                                 CancellationToken cancellationToken)
        {
            using HttpRequestMessage httpRequest = this.BuildHttpRequestMessageWithPayload(restRequest);
            return await this.ExecuteRequestInternalAsync(restRequest, httpRequest, cancellationToken: cancellationToken);
        }

        /// <summary>Preserves the direct overload's payload and preparation-error contract.</summary>
        private Task<RestRequestResult> ExecutePayloadRequestAsync<T>(RestRequest<T> restRequest,
                                                                      CancellationToken cancellationToken)
        {
            return this.httpExecutionPipeline.ExecuteAsync(restRequest,
                                                           () => this.BuildDirectPayloadHttpRequestMessage(restRequest),
                                                           cancellationToken);
        }

        /// <summary>Preserves direct payload serialization separately from response serializer selection.</summary>
        private Task<RestRequestResult<D>> ExecutePayloadRequestAsync<D, T>(RestRequest<T> restRequest,
                                                                            PayloadJsonSerializerLibrary? serializerLibrary,
                                                                            CancellationToken cancellationToken)
        {
            return this.httpExecutionPipeline.ExecuteAsync<D>(restRequest,
                                                              () => this.BuildDirectPayloadHttpRequestMessage(restRequest),
                                                              serializerLibrary,
                                                              cancellationToken);
        }

        /// <summary>Preserves direct bodyless response serializer selection.</summary>
        private Task<RestRequestResult<T>> ExecuteTypedRequestAsync<T>(RestRequest restRequest,
                                                                       PayloadJsonSerializerLibrary? serializerLibrary,
                                                                       CancellationToken cancellationToken)
        {
            return this.httpExecutionPipeline.ExecuteAsync<T>(restRequest,
                                                              () => this.BuildDirectHttpRequestMessage(restRequest),
                                                              serializerLibrary,
                                                              cancellationToken);
        }

        /// <summary>Retains the HttpClient shortcut defaults used by direct convenience methods.</summary>
        private HttpRequestMessage BuildDirectHttpRequestMessage(RestRequest restRequest)
        {
            return new HttpRequestMessage(new NetHttpMethod(restRequest.Method.ToString().ToUpperInvariant()), restRequest.Uri)
            {
                Version = this.httpClientContext.HttpClient.DefaultRequestVersion,
                VersionPolicy = this.httpClientContext.HttpClient.DefaultVersionPolicy
            };
        }

        /// <summary>Includes empty text content for null direct payloads, as in the original shortcuts.</summary>
        private HttpRequestMessage BuildDirectPayloadHttpRequestMessage<T>(RestRequest<T> restRequest)
        {
            HttpRequestMessage httpRequest = this.BuildDirectHttpRequestMessage(restRequest);

            try
            {
                httpRequest.Content = this.BuildJsonHttpContent(restRequest.RequestData,
                                                                payloadJsonSerializerLibrary: restRequest.ForcePayloadJsonSerializerLibrary);
                return httpRequest;
            }
            catch
            {
                httpRequest.Dispose();
                throw;
            }
        }

        /// <summary>Validates a builder before creating an owned context.</summary>
        private static HttpClientContext BuildContext(IHttpClientContextBuilder httpClientBuilder)
        {
            ArgumentNullException.ThrowIfNull(httpClientBuilder);
            return httpClientBuilder.Build();
        }

#endregion
    }
}
