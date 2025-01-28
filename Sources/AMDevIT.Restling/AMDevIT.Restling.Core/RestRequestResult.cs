using AMDevIT.Restling.Core.Text;
using System.Net;

namespace AMDevIT.Restling.Core
{
    public class RestRequestResult(RestRequest request,
                                   HttpStatusCode? statusCode,
                                   TimeSpan elapsed,
                                   byte[] rawContent,
                                   string? contentType,
                                   Charset charset,
                                   object? content = null,
                                   Exception? exception = null)
    {
        #region Fields

        private readonly RestRequest request = request;
        private readonly TimeSpan elapsed = elapsed;
        private readonly HttpStatusCode? statusCode = statusCode;
        private readonly string? contentType = contentType;
        private readonly Charset charSet = charset;
        private readonly byte[]? rawContent = rawContent;
        private readonly object? content = content;
        private readonly Exception? exception = exception;

        #endregion

        #region Properties

        public RestRequest Request => this.request;
        public TimeSpan Elapsed => this.elapsed;
        public HttpStatusCode? StatusCode => this.statusCode;
        public bool IsSuccessful => this.ValidateIsSuccessful();
        public byte[]? RawContent => this.rawContent;
        public object? Content => this.content;

        public string? ContentType => this.contentType;
        public Charset CharSet => this.charSet;

        public Exception? Exception => this.exception;

        #endregion

        #region .ctor

        public RestRequestResult(RestRequest request, Exception exception)
            : this(request, null, TimeSpan.Zero, [], null, Charset.UTF8, null, exception)
        {

        }

        public RestRequestResult(RestRequest request,    
                                 Exception exception,
                                 TimeSpan elapsed)
          : this(request, 
                 null, 
                 elapsed, 
                 [], 
                 null, 
                 Charset.UTF8, 
                 null, 
                 exception)
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
