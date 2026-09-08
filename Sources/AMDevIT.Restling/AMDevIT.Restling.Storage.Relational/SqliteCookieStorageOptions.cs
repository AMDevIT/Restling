using AMDevIT.Restling.Core.Cookies.Storage.Security;

namespace AMDevIT.Restling.Storage.Relational
{
    /// <summary>Configures plain or application-encrypted SQLite cookie persistence.</summary>
    public sealed class SqliteCookieStorageOptions
    {
        #region Properties

        /// <summary>Gets or sets whether cookie changes schedule automatic persistence.</summary>
        public bool AutoSave { get; set; } = true;

        /// <summary>Gets or sets the interval used to coalesce automatic saves.</summary>
        public TimeSpan AutoSaveDelay { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>Gets or sets the protector required by application encryption.</summary>
        public IDataEncryptionKeyProtector? DataEncryptionKeyProtector { get; set; }

        /// <summary>Gets or sets the SQLite database path.</summary>
        public string FilePath { get; set; } = null!;

        /// <summary>Gets or sets the required storage encryption mode.</summary>
        public SqliteCookieEncryptionMode EncryptionMode { get; set; }

        #endregion
    }
}
