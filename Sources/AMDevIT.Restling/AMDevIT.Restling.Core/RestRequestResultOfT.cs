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

        public RestRequestResult(T? data,
                                 HttpStatusCode? statusCode, 
                                 TimeSpan elapsed, 
                                 byte[] rawContent) 
            : base(statusCode, elapsed, rawContent)
        {
            this.data = data;
        }

        public RestRequestResult(Exception exception)
          : base(exception)
        {

        }

        public RestRequestResult(Exception exception,
                                 TimeSpan elapsed)
          : base(exception, elapsed)
        {
        }

        #endregion
    }
}
