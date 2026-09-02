using System.Net;

namespace AMDevIT.Restling.Tests.Pipeline
{
    internal sealed class TrackingResponseContent(byte[] content)
        : HttpContent
    {
        #region Fields

        private readonly byte[] content = content;

        #endregion

        #region Properties

        public bool Disposed { get; private set; }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return stream.WriteAsync(this.content).AsTask();
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = this.content.Length;
            return true;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.Disposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }
}
