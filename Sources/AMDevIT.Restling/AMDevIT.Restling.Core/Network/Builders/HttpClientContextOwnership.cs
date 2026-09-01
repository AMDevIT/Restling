namespace AMDevIT.Restling.Core.Network.Builders
{
    /// <summary>Defines the resources disposed by an HttpClientContext.</summary>
    [Flags]
    public enum HttpClientContextOwnership
    {
        /// <summary>The context does not dispose either resource.</summary>
        None = 0,

        /// <summary>The context disposes the HTTP client.</summary>
        HttpClient = 1,

        /// <summary>The context disposes the message handler.</summary>
        HttpMessageHandler = 2,

        /// <summary>The context disposes both the HTTP client and the message handler.</summary>
        All = HttpClient | HttpMessageHandler
    }
}
