namespace AMDevIT.Restling.Core.Network
{
    /// <summary>Applies the common proxy URI contract used by context and request configuration.</summary>
    internal static class ProxyUriParser
    {
        #region Methods

        /// <summary>Returns a normalized supported proxy URI.</summary>
        public static Uri Parse(string proxyUri)
        {
            Uri? address;

            ArgumentException.ThrowIfNullOrWhiteSpace(proxyUri);
            if (!Uri.TryCreate(proxyUri, UriKind.Absolute, out address) ||
                string.IsNullOrEmpty(address.Host) ||
                address.Scheme is not ("http" or "https" or "socks4" or "socks4a" or "socks5") ||
                address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0 ||
                (address.AbsolutePath.Length != 0 && address.AbsolutePath != "/"))
                throw new ArgumentException("Specify an absolute HTTP, HTTPS, or SOCKS proxy URI containing only a host and optional port. Configure credentials on the transport factory.", nameof(proxyUri));

            return address;
        }

        #endregion
    }
}
