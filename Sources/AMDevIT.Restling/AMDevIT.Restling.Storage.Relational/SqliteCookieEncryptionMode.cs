namespace AMDevIT.Restling.Storage.Relational
{
    /// <summary>Defines how cookie records are persisted in SQLite.</summary>
    public enum SqliteCookieEncryptionMode
    {
        /// <summary>Stores cookie properties in ordinary relational columns.</summary>
        None = 0,

        /// <summary>Stores an HMAC blind index and an AES-256-GCM encrypted cookie payload.</summary>
        Application = 1
    }
}
