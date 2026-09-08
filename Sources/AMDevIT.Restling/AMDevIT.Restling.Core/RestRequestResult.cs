using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Text;
using AMDevIT.Restling.Core.Codecs;
using System.Net;
using System.Security.AccessControl;

namespace AMDevIT.Restling.Core
{
    public class RestRequestResult(RestRequest request,
                                   HttpStatusCode? statusCode,
                                   TimeSpan elapsed,
                                   byte[] rawContent,
                                   string? contentType,
                                   Charset charset,
                                   RetrievedContentResult? retrievedContent,
                                   ResponseHeaders responseHeaders,
                                   Exception? exception = null)
    {
        #region Fields

        private readonly RestRequest request = request;
        private readonly TimeSpan elapsed = elapsed;
        private readonly HttpStatusCode? statusCode = statusCode;
        private readonly string? contentType = contentType;
        private readonly Charset charSet = charset;
        private readonly byte[]? rawContent = rawContent;
        private readonly RetrievedContentResult? retrievedContent = retrievedContent;
        private readonly Exception? exception = exception;
        private readonly ResponseHeaders responseHeaders = responseHeaders;

        #endregion

        #region Properties

        public RestRequest Request => this.request;
        public TimeSpan Elapsed => this.elapsed;
        public HttpStatusCode? StatusCode => this.statusCode;
        public bool IsSuccessful => this.ValidateIsSuccessful();
        public byte[]? RawContent => this.rawContent;
        public RetrievedContentResult? RetrievedContent => this.retrievedContent;
        public string? Content => this.retrievedContent?.ToStringContent();

        public string? ContentType => this.contentType;
        public Charset CharSet => this.charSet;
        public Exception? Exception => this.exception;

        /// <summary>Structured problem information when an optional problem codec is registered.</summary>
        public RestProblemDetails? Problem { get; internal set; }

        /// <summary>A problem-document decoding failure, without replacing the HTTP status or original body.</summary>
        public Exception? ProblemException { get; internal set; }

        /// <summary>Gets response data decoded through the mapping selected for the HTTP status code.</summary>
        public object? MappedData { get; internal set; }

        /// <summary>Gets the registered type selected for the HTTP status code.</summary>
        public Type? MappedDataType { get; internal set; }

        /// <summary>Gets a mapped-response decoding failure without replacing the HTTP result.</summary>
        public Exception? MappedDataException { get; internal set; }

        public ResponseHeaders ResponseHeaders => this.responseHeaders;

        #endregion

        #region .ctor

        public RestRequestResult(RestRequest request, Exception exception)
            : this(request, null, TimeSpan.Zero, [], null, Charset.UTF8, null, ResponseHeaders.Empty, exception)
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
                 ResponseHeaders.Empty,
                 exception)
        {
        }

        #endregion

        #region Methods

        /// <summary>Attempts to retrieve mapped response data as the requested type.</summary>
        /// <typeparam name="T">The expected mapped response data type.</typeparam>
        /// <param name="data">The mapped response data when its runtime type is compatible.</param>
        /// <returns>True when mapped response data is available as the requested type.</returns>
        public bool TryGetMappedData<T>(out T? data)
        {
            if (this.MappedData is T mappedData)
            {
                data = mappedData;
                return true;
            }

            data = default;
            return false;
        }

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
