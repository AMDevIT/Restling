using System.Net;

namespace AMDevIT.Restling.Core
{
    public class RestRequestResult(HttpStatusCode? statusCode,
                                   TimeSpan elapsed,
                                   byte[] rawContent,
                                   Exception? exception = null)
    {
        #region Fields

        private readonly TimeSpan elapsed = elapsed;
        private readonly HttpStatusCode? statusCode = statusCode;
        private readonly byte[]? rawContent = rawContent;
        private readonly Exception? exception = exception;

        #endregion

        #region Properties

        public TimeSpan Elapsed => this.elapsed;
        public HttpStatusCode? StatusCode => this.statusCode;
        public bool IsSuccessful => this.ValidateIsSuccessful();
        public byte[]? RawContent => this.rawContent;

        public Exception? Exception => this.exception;

        #endregion

        #region .ctor

        public RestRequestResult(Exception exception)
            : this(null, TimeSpan.Zero, [], exception)
        {

        }

        public RestRequestResult(Exception exception,
                                 TimeSpan elapsed)
          : this(null, elapsed, [], exception)
        {
        }

        #endregion

        #region Methods

        protected virtual bool ValidateIsSuccessful()
        {
            if (this.exception != null)
                return false;

            bool isSuccessful = this.statusCode switch
            {
                HttpStatusCode.OK => true,
                HttpStatusCode.Created => true,
                HttpStatusCode.Accepted => true,
                HttpStatusCode.NonAuthoritativeInformation => true,
                HttpStatusCode.NoContent => true,
                HttpStatusCode.ResetContent => true,
                HttpStatusCode.PartialContent => true,
                HttpStatusCode.MultiStatus => true,
                HttpStatusCode.AlreadyReported => true,
                HttpStatusCode.IMUsed => true,
                _ => false
            };

            return isSuccessful;
        }

        #endregion
    }
}
