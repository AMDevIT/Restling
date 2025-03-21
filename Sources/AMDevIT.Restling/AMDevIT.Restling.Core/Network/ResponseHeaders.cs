using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Network
{
    public class ResponseHeaders
    {
        #region Fields

        private static readonly ResponseHeaders empty = new();

        private readonly ReadOnlyDictionary<string, IEnumerable<string>> headers;
        private readonly string? redirectLocation;

        #endregion

        #region Properties

        public static ResponseHeaders Empty => empty;

        public ReadOnlyDictionary<string, IEnumerable<string>> Headers => this.headers;

        public string? RedirectLocation => this.redirectLocation;

        #endregion

        #region .ctor      

        protected ResponseHeaders()
        {
            Dictionary<string, IEnumerable<string>> headers = [];
            this.headers = new ReadOnlyDictionary<string, IEnumerable<string>>(headers);
            this.redirectLocation = null;
        }

        protected ResponseHeaders(HttpResponseHeaders sourceResponseHeaders)
        {
            Dictionary<string, IEnumerable<string>> headers = [];

            foreach (KeyValuePair<string, IEnumerable<string>> header in sourceResponseHeaders)
            {
                headers.Add(header.Key, header.Value);
            }

            this.headers = new ReadOnlyDictionary<string, IEnumerable<string>>(headers);
            this.redirectLocation = sourceResponseHeaders.Location?.ToString();
        }

        #endregion

        #region Methods

        public static ResponseHeaders Create(HttpResponseHeaders httpResponseMessage)
        {
            ResponseHeaders responseHeaders = new(httpResponseMessage);
            return responseHeaders;
        }

        public override string ToString()
        {
            string headers;

            if (this.headers.Count > 0)
                headers = string.Join(", ", this.headers.Select(h => $"{h.Key}: {h.Value}"));
            else
                headers = "No headers";

            return $"Headers: {headers} - Redirect location: {this.redirectLocation ?? "No redirect location provided"}";
        }

        #endregion
    }
}
