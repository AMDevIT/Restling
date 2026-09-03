namespace AMDevIT.Restling.Core.Network
{
    /// <summary>Identifies how an individual request reaches its destination.</summary>
    public enum RequestProxyMode
    {
        /// <summary>Uses the HttpClientContext transport without creating an alternative transport.</summary>
        Default,

        /// <summary>Connects directly and ignores both explicit and system proxy settings.</summary>
        Direct,

        /// <summary>Uses the proxy selected for the individual request.</summary>
        Custom
    }
}
