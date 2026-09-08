namespace AMDevIT.Restling.Storage.Json
{
    /// <summary>Configures versioned JSON cookie persistence.</summary>
    public sealed class JsonCookieStorageOptions
    {
        #region Properties

        /// <summary>Gets or sets whether cookie changes schedule an automatic save.</summary>
        public bool AutoSave { get; set; } = true;

        /// <summary>Gets or sets the interval used to coalesce automatic saves.</summary>
        public TimeSpan AutoSaveDelay { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>Gets or sets the optional protector used for AES-256-GCM data-encryption keys.</summary>
        public IDataEncryptionKeyProtector? DataEncryptionKeyProtector { get; set; }

        /// <summary>Gets or sets the JSON storage file path.</summary>
        public string FilePath { get; set; } = null!;

        #endregion
    }
}
