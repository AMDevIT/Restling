namespace AMDevIT.Restling.Core.Network.Pipeline
{
    /// <summary>Owns a streaming HTTP response until its consumer finishes reading it.</summary>
    internal sealed class HttpResponseLease : IDisposable
    {
        #region Properties

        public HttpResponseMessage Response { get; }
        public TimeSpan Elapsed { get; }

        #endregion

        #region .ctor

        /// <summary>Accepts ownership of a response that has not been buffered.</summary>
        public HttpResponseLease(HttpResponseMessage response, TimeSpan elapsed)
        {
            this.Response = response;
            this.Elapsed = elapsed;
        }

        #endregion

        #region Methods

        /// <summary>Releases the response and its content when streaming finishes.</summary>
        public void Dispose()
        {
            this.Response.Dispose();
        }

        #endregion
    }
}
