using AMDevIT.Restling.Core.Text;
using System.Net;

namespace AMDevIT.Restling.Core
{
    public class RestRequestResult(HttpStatusCode? statusCode,
                                   TimeSpan elapsed,
                                   byte[] rawContent,
                                   string? contentType,
                                   Charset charset,
                                   string? content = null,
                                   Exception? exception = null)
    {
        #region Fields

        private readonly TimeSpan elapsed = elapsed;
        private readonly HttpStatusCode? statusCode = statusCode;
        private readonly string? contentType = contentType;
        private readonly Charset charSet = charset;
        private readonly byte[]? rawContent = rawContent;
        private readonly string? content = content;
        private readonly Exception? exception = exception;

        #endregion

        #region Properties

        public TimeSpan Elapsed => this.elapsed;
        public HttpStatusCode? StatusCode => this.statusCode;
        public bool IsSuccessful => this.ValidateIsSuccessful();
        public byte[]? RawContent => this.rawContent;
        public string? Content => this.content;

        public string? ContentType => this.contentType;
        public Charset CharSet => this.charSet;

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

        public override string ToString()
        {
            return $"[Status:{this.StatusCode},Elapsed:{this.Elapsed}]{this.ContentType}({this.CharSet})";
        }

        #endregion
    }
}
