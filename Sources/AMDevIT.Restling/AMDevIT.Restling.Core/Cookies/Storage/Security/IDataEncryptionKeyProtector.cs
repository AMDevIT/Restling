namespace AMDevIT.Restling.Core.Cookies.Storage.Security
{
    /// <summary>Protects random data-encryption keys with a key held outside persisted cookie storage.</summary>
    public interface IDataEncryptionKeyProtector
    {
        #region Properties

        /// <summary>Gets the identifier recorded with newly protected keys.</summary>
        string KeyId { get; }

        #endregion

        #region Methods

        /// <summary>Protects a newly generated 256-bit data-encryption key.</summary>
        ValueTask<byte[]> ProtectAsync(ReadOnlyMemory<byte> dataEncryptionKey,
                                       CancellationToken cancellationToken = default);

        /// <summary>Unprotects a stored data-encryption key.</summary>
        ValueTask<byte[]> UnprotectAsync(string keyId,
                                         ReadOnlyMemory<byte> protectedDataEncryptionKey,
                                         CancellationToken cancellationToken = default);

        #endregion
    }
}
