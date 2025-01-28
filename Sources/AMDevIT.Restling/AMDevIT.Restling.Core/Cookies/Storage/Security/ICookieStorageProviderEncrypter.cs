namespace AMDevIT.Restling.Core.Cookies.Storage.Security
{
    public interface ICookieStorageProviderEncrypter
    {
        #region Methods
        Task<string> EncryptAsync(string text, CancellationToken cancellationToken = default);

        Task<string> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default);        

        #endregion
    }
}
