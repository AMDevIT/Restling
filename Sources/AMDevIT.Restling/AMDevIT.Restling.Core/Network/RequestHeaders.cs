namespace AMDevIT.Restling.Core.Network
{
    public class RequestHeaders(AuthenticationHeader? authenticationHeader = null)
    {
        #region Fields

        private AuthenticationHeader? authenticationHeader = authenticationHeader;
        private readonly Dictionary<string, string> headers = [];

        #endregion

        #region Properties

        public AuthenticationHeader? AuthenticationHeader
        {
            get => this.authenticationHeader;
            set => this.authenticationHeader = value;
        }

        public Dictionary<string, string> Headers => this.headers;

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{this.authenticationHeader?.ToString() ?? "No authentication header"}" +
                   $"{string.Join(Environment.NewLine, this.headers.Select(h => $"{h.Key}: {h.Value}"))}";
        }

        #endregion
    }
}
