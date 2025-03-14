﻿using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Serialization;

namespace AMDevIT.Restling.Core
{
    /// <summary>
    /// Represents a REST client that can be used to perform HTTP requests to a remote resource.
    /// </summary>
    public interface IRestlingClient
    {
        #region Properties

        public HttpClientContext ClientContext
        {
            get;
        }

        PayloadJsonSerializerLibrary SelectedDefaultSerializationLibrary
        {
            get;
            set;
        }

        /// <summary>
        /// Dispose the HttpClient instance and all the handlers when disposing the RestlingClient instance.
        /// </summary>
        bool DisposeContext
        {
            get;
            set;
        }

        bool Disposed
        {
            get;
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
        Task<RestRequestResult> GetAsync(string uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a GET request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                               CancellationToken cancellationToken = default);

        Task<RestRequestResult> GetAsync(string uri,
                                         RequestHeaders requestHeaders,
                                         CancellationToken cancellationToken = default);

        Task<RestRequestResult<T>> GetAsync<T>(string uri,
                                               RequestHeaders requestHeaders,
                                               PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                               CancellationToken cancellationToken = default);

        #endregion

        #region POST

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <typeparam name="T">The request body payload model type</typeparam>        
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult> PostAsync<T>(string uri,
                                             T requestData,
                                             PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                             CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a POST request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The request body payload model type</typeparam>
        /// <typeparam name="D">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                   T requestData,
                                                   PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                   CancellationToken cancellationToken = default);

        Task<RestRequestResult> PostAsync<T>(string uri,
                                             T requestData,
                                             RequestHeaders requestHeaders,
                                             PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                             CancellationToken cancellationToken = default);

        Task<RestRequestResult<D>> PostAsync<D, T>(string uri,
                                                   T requestData,
                                                   RequestHeaders requestHeaders,
                                                   PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                   CancellationToken cancellationToken = default);

        #endregion

        #region PUT

        Task<RestRequestResult> PutAsync<T>(string uri,
                                            T requestData,
                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a PUT request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The request body payload model type</typeparam>
        /// <typeparam name="D">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                  T requestData,
                                                  PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                  CancellationToken cancellationToken = default);

        Task<RestRequestResult> PutAsync<T>(string uri,
                                            T requestData,
                                            RequestHeaders requestHeaders,
                                            PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                            CancellationToken cancellationToken = default);

        Task<RestRequestResult<D>> PutAsync<D, T>(string uri,
                                                  T requestData,
                                                  RequestHeaders requestHeaders,
                                                  PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                  CancellationToken cancellationToken = default);

        #endregion

        #region DELETE

        /// <summary>
        /// Execute a DELETE request to the specified URI and return the result as a <see cref="RestRequestResult"/> instance.
        /// </summary>
        /// <param name="uri">The request resource URI</param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult> DeleteAsync(string uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a DELETE request to the specified URI and return the result as a <see cref="RestRequestResult{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">A primitive type or a model to which the result content body will be parsed to</typeparam>
        /// <param name="uri">The request resource URI</param>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken">A valid cancellation token</param>
        /// <returns>The value returned from the remote resource</returns>
        Task<RestRequestResult<T>> DeleteAsync<T>(string uri, 
                                                  PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null, 
                                                  CancellationToken cancellationToken = default);

        Task<RestRequestResult> DeleteAsync(string uri,
                                            RequestHeaders requestHeaders,
                                            CancellationToken cancellationToken = default);

        Task<RestRequestResult<T>> DeleteAsync<T>(string uri,
                                                  RequestHeaders requestHeaders,
                                                  PayloadJsonSerializerLibrary? forcePayloadJsonSerializerLibrary = null,
                                                  CancellationToken cancellationToken = default);

        #endregion


        #region Generic execution methods

        Task<RestRequestResult> ExecuteRequestAsync(RestRequest restRequest,
                                                    bool throwOnGenerics = true,
                                                    CancellationToken cancellationToken = default);
        Task<RestRequestResult<T>> ExecuteRequestAsync<T>(RestRequest restRequest,
                                                          bool throwOnGenerics = false,
                                                          CancellationToken cancellationToken = default);
        Task<RestRequestResult<D>> ExecuteRequestAsync<D, T>(RestRequest<T> restRequest,
                                                             bool throwOnGenerics = false,
                                                             CancellationToken cancellationToken = default);

        #endregion

        #endregion
    }
}
