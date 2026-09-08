namespace AMDevIT.Restling.Core
{
    /// <summary>Defines whether a Restling client owns the context supplied to it.</summary>
    public enum RestlingClientContextOwnership
    {
        /// <summary>The context remains owned by the caller.</summary>
        Borrowed,

        /// <summary>The context is disposed together with the client.</summary>
        Owned
    }
}
