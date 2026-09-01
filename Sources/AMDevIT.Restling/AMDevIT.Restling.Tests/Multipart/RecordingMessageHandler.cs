namespace AMDevIT.Restling.Tests.Multipart
{
    internal sealed class RecordingMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        #region Fields

        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory = responseFactory;

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.responseFactory(request, cancellationToken);
        }

        #endregion
    }
}
