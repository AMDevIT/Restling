using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AMDevIT.Restling.Core
{
    public class HttpClientException
        : Exception
    {
        #region .ctor

        public HttpClientException()
        {
        }

        public HttpClientException(string? message) : base(message)
        {
        }

        public HttpClientException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        #endregion
    }
}
