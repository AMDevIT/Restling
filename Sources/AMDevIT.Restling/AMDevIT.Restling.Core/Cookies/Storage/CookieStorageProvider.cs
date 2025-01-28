using AMDevIT.Restling.Core.Cookies.Storage.Security;
using AMDevIT.Restling.Core.Cookies.Storage.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace AMDevIT.Restling.Core.Cookies.Storage
{
    public class CookieStorageProvider(string cookieStorageFilePath,
                                       ILogger? logger = null,
                                       ICookieStorageProviderEncrypter? encrypter = null)
        : ICookiesStorageProvider
    {
        #region Fields

        private readonly ILogger? logger = logger;
        private readonly string cookieStorageFilePath = cookieStorageFilePath;
        private readonly ICookieStorageProviderEncrypter? encrypter = encrypter;
        private readonly HashSet<HttpCookieData> cookies = [];

        #endregion

        #region Properties

        public bool Encrypted
        {
            get;
            set;
        }

        public string CookieStorageFilePath => this.cookieStorageFilePath;

        protected ILogger? Logger => this.logger;

        #endregion

        #region Methods

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(this.CookieStorageFilePath))
                throw new FileNotFoundException("Cannot find cookie storage file", this.CookieStorageFilePath);

            string contentString = await File.ReadAllTextAsync(this.CookieStorageFilePath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(contentString))
            {
                CookieSerializationItem[] cookieSerializedItems;

                if (this.Encrypted)
                {
                    if (this.encrypter == null)
                        throw new InvalidOperationException("Cannot decrypt the cookie storage content without a valid encrypter");

                    contentString = await this.encrypter.DecryptAsync(contentString, cancellationToken);
                }
                // Deserialize the content string to a list of HttpCookieData instances
                // and store them in the internal collection

                cookieSerializedItems = JsonConvert.DeserializeObject<CookieSerializationItem[]>(contentString) ?? [];
                foreach (CookieSerializationItem cookieSerializedItem in cookieSerializedItems)
                {
                    bool result;
                    HttpCookieData cookie = cookieSerializedItem.ToCookieData();
                    result = await this.AddCookieAsync(cookie, cancellationToken);
                    this.Logger?.LogTrace("Add cookie {cookie} to storage result: {result}", cookie, result);
                }
            }
            else
            {
                this.Logger?.LogInformation("The cookie storage file is empty");
            }
        }
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            string contentString;
            List<CookieSerializationItem> cookieSerializedItems = [];

            foreach(HttpCookieData cookie in this.cookies)
            {
                CookieSerializationItem cookieSerializationItem = cookie.ToSerializationItem();
                cookieSerializedItems.Add(cookieSerializationItem);
            }

            contentString = JsonConvert.SerializeObject(cookieSerializedItems);

            if (this.Encrypted == true)
            {
                if (this.encrypter == null)
                    throw new InvalidOperationException("Cannot encrypt the cookie storage content without a valid encrypter");
                contentString = await this.encrypter.EncryptAsync(contentString, cancellationToken);
            }

            using StreamWriter fileWriter = File.CreateText(this.CookieStorageFilePath);
            await fileWriter.WriteAsync(contentString.AsMemory(), cancellationToken);
        }

        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            if (this.cookies.TryGetValue(cookie, out HttpCookieData? existingCookie))
            {
                this.cookies.Remove(existingCookie);
            }
            this.cookies.Add(cookie);
            return Task<bool>.FromResult(true);
        }

        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            if (this.cookies.TryGetValue(cookie, out HttpCookieData? existingCookie))
            {
                this.cookies.Remove(existingCookie);
                return Task<bool>.FromResult(true);
            }
            return Task<bool>.FromResult(false);
        }

        public CookieContainer BuildCookieContainer()
        {
            CookieContainer cookieContainer = new ();

            foreach (HttpCookieData cookie in this.cookies)
            {
                Cookie newCookie = new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                cookieContainer.Add(newCookie);
            }

            return cookieContainer;
        }

        #endregion
    }
}
