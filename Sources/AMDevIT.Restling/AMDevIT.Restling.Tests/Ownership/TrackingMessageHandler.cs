using System.Net;

namespace AMDevIT.Restling.Tests.Ownership
{
    internal sealed class TrackingMessageHandler : HttpMessageHandler
    {
        #region Properties

        public bool Disposed { get; private set; }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.Disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}
