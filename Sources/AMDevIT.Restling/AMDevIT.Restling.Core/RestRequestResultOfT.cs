using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Text;
using System.Net;

namespace AMDevIT.Restling.Core
{
    public class RestRequestResult<T>
        : RestRequestResult
    {
        #region Fields

        private readonly T? data;

        #endregion

        #region Properties

        public T? Data => this.data;

        #endregion

        #region .ctor

        public RestRequestResult(RestRequest request,
                                 T? data,
                                 HttpStatusCode? statusCode, 
                                 TimeSpan elapsed, 
                                 byte[] rawContent,
                                 string? contentType,
                                 Charset charset,
                                 RetrievedContentResult? retrievedContent,
                                 ResponseHeaders responseHeaders,
                                 Exception? exception = null) 
            : base(request,
                   statusCode, 
                   elapsed, 
                   rawContent, 
                   contentType,
                   charset,
                   retrievedContent,                   
                   responseHeaders,
                   exception)
        {
            this.data = data;
        }

        public RestRequestResult(RestRequest request, Exception exception)
          : base(request, exception)
        {

        }

        public RestRequestResult(RestRequest request, 
                                 Exception exception,
                                 TimeSpan elapsed)
          : base(request, 
                 exception, 
                 elapsed)
        {
        }

        #endregion
    }
}
