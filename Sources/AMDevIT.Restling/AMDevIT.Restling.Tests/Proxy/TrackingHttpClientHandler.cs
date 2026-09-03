namespace AMDevIT.Restling.Tests.Proxy
{
    /// <summary>Observes handler ownership without changing native transport behavior.</summary>
    internal sealed class TrackingHttpClientHandler : HttpClientHandler
    {
        #region Properties

        public bool Disposed { get; private set; }

        #endregion

        #region Methods

        /// <summary>Records disposal before releasing native resources.</summary>
        protected override void Dispose(bool disposing)
        {
            this.Disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}
