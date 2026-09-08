using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AMDevIT.Restling.Core.Network.Pipeline
{
    /// <summary>Centralizes finite and streaming HTTP execution behavior.</summary>
    internal sealed class HttpExecutionPipeline
    {
        #region Fields

        private readonly Func<RequestProxyOptions?, HttpClient> httpClientResolver;
        private readonly Action<RequestProxyOptions?, HttpClient> httpClientInvalidator;
        private readonly ContentCodecRegistry codecs;
        private readonly ILogger? logger;

        #endregion

        #region .ctor

        /// <summary>Creates a pipeline borrowing its transport and immutable codec registry.</summary>
        public HttpExecutionPipeline(Func<RequestProxyOptions?, HttpClient> httpClientResolver,
                                     Action<RequestProxyOptions?, HttpClient> httpClientInvalidator,
                                     ContentCodecRegistry codecs,
                                     ILogger? logger)
        {
            this.httpClientResolver = httpClientResolver;
            this.httpClientInvalidator = httpClientInvalidator;
            this.codecs = codecs;
            this.logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>Sends a prepared request and buffers its untyped response.</summary>
        public Task<RestRequestResult> ExecuteAsync(RestRequest restRequest,
                                                    HttpRequestMessage httpRequest,
                                                    CancellationToken cancellationToken)
        {
            return this.ExecuteCoreAsync(restRequest,
                                         httpRequest,
                                         (parser, response, elapsed, token) => parser.DecodeAsync(response,
                                                                                                  restRequest,
                                                                                                  elapsed,
                                                                                                  token),
                                         (exception, elapsed) => new RestRequestResult(restRequest, exception, elapsed),
                                         cancellationToken);
        }

        /// <summary>Sends a prepared request and decodes its buffered typed response.</summary>
        public Task<RestRequestResult<T>> ExecuteAsync<T>(RestRequest restRequest,
                                                          HttpRequestMessage httpRequest,
                                                          PayloadJsonSerializerLibrary? serializerLibrary,
                                                          CancellationToken cancellationToken)
        {
            return this.ExecuteCoreAsync(restRequest,
                                         httpRequest,
                                         (parser, response, elapsed, token) => parser.DecodeAsync<T>(response,
                                                                                                     restRequest,
                                                                                                     elapsed,
                                                                                                     serializerLibrary,
                                                                                                     token),
                                         (exception, elapsed) => new RestRequestResult<T>(restRequest, exception, elapsed),
                                         cancellationToken);
        }

        /// <summary>Preserves result-based preparation failures for direct untyped convenience methods.</summary>
        public Task<RestRequestResult> ExecuteAsync(RestRequest restRequest,
                                                    Func<HttpRequestMessage> requestFactory,
                                                    CancellationToken cancellationToken)
        {
            return this.PrepareAndExecuteAsync(restRequest,
                                               requestFactory,
                                               request => this.ExecuteAsync(restRequest, request, cancellationToken),
                                               exception => new RestRequestResult(restRequest, exception, TimeSpan.Zero));
        }

        /// <summary>Preserves result-based preparation failures for direct typed convenience methods.</summary>
        public Task<RestRequestResult<T>> ExecuteAsync<T>(RestRequest restRequest,
                                                          Func<HttpRequestMessage> requestFactory,
                                                          PayloadJsonSerializerLibrary? serializerLibrary,
                                                          CancellationToken cancellationToken)
        {
            return this.PrepareAndExecuteAsync(restRequest,
                                               requestFactory,
                                               request => this.ExecuteAsync<T>(restRequest, request, serializerLibrary, cancellationToken),
                                               exception => new RestRequestResult<T>(restRequest, exception, TimeSpan.Zero));
        }

        /// <summary>Sends without buffering and transfers response ownership to a streaming lease.</summary>
        public async Task<HttpResponseLease> SendStreamingAsync(RestRequest restRequest,
                                                                HttpRequestMessage httpRequest,
                                                                CancellationToken cancellationToken)
        {
            HttpClient? httpClient = null;
            HttpResponseMessage? response = null;
            Stopwatch stopwatch = new();

            try
            {
                this.LogStart(httpRequest);
                stopwatch.Start();
                httpClient = this.httpClientResolver(restRequest.ProxyOptions);
                response = await httpClient.SendAsync(httpRequest,
                                                      HttpCompletionOption.ResponseHeadersRead,
                                                      cancellationToken);
                stopwatch.Stop();
                this.LogCompleted(httpRequest, stopwatch.Elapsed);
                return new HttpResponseLease(response, stopwatch.Elapsed);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                this.DisposeResponse(response);
                this.InvalidatePrematureResponseTransport(restRequest.ProxyOptions, httpClient, exception);
                this.LogFailure(httpRequest, exception);
                throw;
            }
        }

        /// <summary>Owns requests created for convenience methods without catching decoder exceptions.</summary>
        private async Task<TResult> PrepareAndExecuteAsync<TResult>(RestRequest restRequest,
                                                                    Func<HttpRequestMessage> requestFactory,
                                                                    Func<HttpRequestMessage, Task<TResult>> execute,
                                                                    Func<Exception, TResult> failureFactory)
        {
            HttpRequestMessage httpRequest;

            try
            {
                httpRequest = requestFactory();
            }
            catch (Exception exception)
            {
                this.logger?.LogError(exception, "Cannot prepare {method} REST request.", restRequest.Method);
                return failureFactory(exception);
            }

            try
            {
                return await execute(httpRequest);
            }
            finally
            {
                try
                {
                    httpRequest.Dispose();
                }
                catch (Exception exception)
                {
                    this.logger?.LogTrace(exception, "Cannot dispose the HttpRequestMessage instance.");
                }
            }
        }

        /// <summary>Measures buffered transport time, retains send failures and always releases responses.</summary>
        private async Task<TResult> ExecuteCoreAsync<TResult>(RestRequest restRequest,
                                                              HttpRequestMessage httpRequest,
                                                              Func<HttpResponseParser, HttpResponseMessage, TimeSpan, CancellationToken, Task<TResult>> decoder,
                                                              Func<Exception, TimeSpan, TResult> failureFactory,
                                                              CancellationToken cancellationToken)
        {
            HttpClient? httpClient = null;
            HttpResponseMessage? response = null;
            Stopwatch stopwatch = new();

            try
            {
                this.LogStart(httpRequest);
                stopwatch.Start();
                httpClient = this.httpClientResolver(restRequest.ProxyOptions);
                response = await httpClient.SendAsync(httpRequest, cancellationToken);
                stopwatch.Stop();
                this.LogCompleted(httpRequest, stopwatch.Elapsed);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                this.DisposeResponse(response);
                this.InvalidatePrematureResponseTransport(restRequest.ProxyOptions, httpClient, exception);
                this.LogFailure(httpRequest, exception);
                return failureFactory(exception, stopwatch.Elapsed);
            }

            try
            {
                HttpResponseParser parser = new(this.logger) { Codecs = this.codecs };
                return await decoder(parser, response, stopwatch.Elapsed, cancellationToken);
            }
            finally
            {
                this.DisposeResponse(response);
            }
        }

        /// <summary>Evicts a request-specific transport whose response framing ended prematurely.</summary>
        private void InvalidatePrematureResponseTransport(RequestProxyOptions? options,
                                                          HttpClient? httpClient,
                                                          Exception exception)
        {
            Exception? currentException = exception;

            if (httpClient == null)
                return;
            while (currentException != null)
            {
                if (currentException is HttpIOException httpException &&
                    httpException.HttpRequestError == HttpRequestError.ResponseEnded)
                {
                    try
                    {
                        this.httpClientInvalidator(options, httpClient);
                    }
                    catch (Exception invalidationException)
                    {
                        this.logger?.LogTrace(invalidationException, "Cannot invalidate the failed request transport.");
                    }
                    return;
                }
                currentException = currentException.InnerException;
            }
        }

        /// <summary>Releases a response without replacing the HTTP outcome with a disposal failure.</summary>
        private void DisposeResponse(HttpResponseMessage? response)
        {
            try
            {
                response?.Dispose();
            }
            catch (Exception exception)
            {
                this.logger?.LogTrace(exception, "Cannot dispose the HttpResponseMessage instance.");
            }
        }

        /// <summary>Logs the outgoing method without including potentially sensitive URI parameters.</summary>
        private void LogStart(HttpRequestMessage request)
        {
            this.logger?.LogDebug("Executing {method} REST request.", request.Method.Method);
        }

        /// <summary>Logs the transport duration consistently across request paths.</summary>
        private void LogCompleted(HttpRequestMessage request, TimeSpan elapsed)
        {
            this.logger?.LogDebug("{method} REST request executed in {elapsed} ms.",
                                  request.Method.Method,
                                  elapsed.TotalMilliseconds);
        }

        /// <summary>Logs a transport failure consistently across request paths.</summary>
        private void LogFailure(HttpRequestMessage request, Exception exception)
        {
            this.logger?.LogError(exception, "Cannot execute {method} REST request.", request.Method.Method);
        }

        #endregion
    }
}
