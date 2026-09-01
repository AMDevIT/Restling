namespace AMDevIT.Restling.Core.Network.Builders
{
    /// <summary>Defines whether a builder-created context borrows or owns a supplied handler.</summary>
    public enum HttpMessageHandlerOwnership
    {
        /// <summary>The supplied handler remains owned by the caller.</summary>
        Borrowed,

        /// <summary>The context created by the builder owns the supplied handler.</summary>
        Owned
    }
}
