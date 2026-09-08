namespace AMDevIT.Restling.Core.Network
{
    /// <summary>Defines an immutable transport override for an individual request.</summary>
    public sealed record RequestProxyOptions
    {
        #region Properties

        /// <summary>Gets the context-wide transport selection.</summary>
        public static RequestProxyOptions Default { get; } = new(RequestProxyMode.Default, null, false);

        /// <summary>Gets the selected routing mode.</summary>
        public RequestProxyMode Mode { get; }

        /// <summary>Gets the custom proxy address, when Mode is Custom.</summary>
        public Uri? ProxyUri { get; }

        /// <summary>Gets whether the alternative transport follows HTTP response redirects.</summary>
        public bool AllowAutoRedirect { get; }

        #endregion

        #region .ctor

        /// <summary>Creates validated request proxy options.</summary>
        private RequestProxyOptions(RequestProxyMode mode, Uri? proxyUri, bool allowAutoRedirect)
        {
            this.Mode = mode;
            this.ProxyUri = proxyUri;
            this.AllowAutoRedirect = allowAutoRedirect;
        }

        #endregion

        #region Methods

        /// <summary>Creates an override that connects without using an explicit or system proxy.</summary>
        /// <param name="allowAutoRedirect">Whether HTTP response redirects are followed automatically.</param>
        public static RequestProxyOptions Direct(bool allowAutoRedirect = false)
        {
            return new RequestProxyOptions(RequestProxyMode.Direct, null, allowAutoRedirect);
        }

        /// <summary>Creates an override that uses a dedicated proxy.</summary>
        /// <param name="proxyUri">An absolute supported proxy URI without embedded credentials.</param>
        /// <param name="allowAutoRedirect">Whether HTTP response redirects are followed automatically.</param>
        public static RequestProxyOptions Custom(string proxyUri, bool allowAutoRedirect = false)
        {
            Uri address = ProxyUriParser.Parse(proxyUri);
            return new RequestProxyOptions(RequestProxyMode.Custom, address, allowAutoRedirect);
        }

        #endregion
    }
}
