using AMDevIT.Restling.Core.Cookies.Storage.Security;
using AMDevIT.Restling.Core.Cookies.Storage.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace AMDevIT.Restling.Core.Cookies.Storage
{
    /// <summary>Provides an in-memory cookie jar with optional basic JSON file persistence.</summary>
    /// <remarks>This provider is intended primarily for in-memory use. Its optional JSON storage is basic and is not a secure store.</remarks>
    public class CookieStorageProvider : ICookiesStorageProvider
    {
        #region Fields

        private readonly ILogger? logger;
        private readonly string? cookieStorageFilePath;
        private readonly ICookieStorageProviderEncrypter? encrypter;
        private readonly HashSet<HttpCookieData> cookies = [];
        private readonly CookieContainer cookieContainer;

        #endregion

        #region Properties

        public bool Encrypted
        {
            get;
            set;
        }

        public string? CookieStorageFilePath => this.cookieStorageFilePath;

        /// <summary>Gets the live in-memory cookie jar.</summary>
        public CookieContainer CookieContainer => this.cookieContainer;

        protected ILogger? Logger => this.logger;

        #endregion

        #region .ctor

        /// <summary>Creates an in-memory cookie provider without file persistence.</summary>
        public CookieStorageProvider()
            : this(new CookieContainer())
        {
        }

        /// <summary>Creates an in-memory provider around an existing live cookie jar.</summary>
        /// <param name="cookieContainer">The cookie container to expose.</param>
        public CookieStorageProvider(CookieContainer cookieContainer)
        {
            ArgumentNullException.ThrowIfNull(cookieContainer);
            this.cookieContainer = cookieContainer;
        }

        /// <summary>Creates an in-memory cookie provider with optional basic JSON file persistence.</summary>
        /// <param name="cookieStorageFilePath">The JSON file used by explicit load and save operations.</param>
        /// <param name="logger">An optional logger.</param>
        /// <param name="encrypter">An optional legacy text encrypter.</param>
        public CookieStorageProvider(string cookieStorageFilePath,
                                     ILogger? logger = null,
                                     ICookieStorageProviderEncrypter? encrypter = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cookieStorageFilePath);
            this.cookieContainer = new CookieContainer();
            this.cookieStorageFilePath = cookieStorageFilePath;
            this.logger = logger;
            this.encrypter = encrypter;
        }

        #endregion

        #region Methods

        /// <summary>Loads cookies from the configured basic JSON file.</summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (this.CookieStorageFilePath == null)
                return;
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
        /// <summary>Saves cookies to the configured basic JSON file only when called explicitly.</summary>
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            string contentString;
            List<CookieSerializationItem> cookieSerializedItems = [];

            if (this.CookieStorageFilePath == null)
                return;

            foreach (Cookie cookie in this.CookieContainer.GetAllCookies())
            {
                CookieSerializationItem cookieSerializationItem = new(cookie.Domain,
                                                                       cookie.Path,
                                                                       null,
                                                                       cookie.Secure,
                                                                       cookie.Name,
                                                                       cookie.Value);
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

        /// <summary>Adds or replaces a cookie in the live in-memory jar.</summary>
        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            Cookie newCookie;

            ArgumentNullException.ThrowIfNull(cookie);
            if (this.cookies.TryGetValue(cookie, out HttpCookieData? existingCookie))
            {
                this.cookies.Remove(existingCookie);
            }
            this.cookies.Add(cookie);
            newCookie = new Cookie(cookie.Name, cookie.Value, cookie.Path ?? "/", cookie.Domain ?? string.Empty)
            {
                Secure = cookie.IsSecure ?? false
            };
            if (!string.IsNullOrWhiteSpace(cookie.Uri))
                this.CookieContainer.Add(new Uri(cookie.Uri), newCookie);
            else if (!string.IsNullOrWhiteSpace(cookie.Domain))
                this.CookieContainer.Add(newCookie);
            else
                throw new ArgumentException("A cookie domain or URI is required.", nameof(cookie));
            return Task<bool>.FromResult(true);
        }

        /// <summary>Removes a cookie from the live in-memory jar.</summary>
        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            bool removed = false;

            ArgumentNullException.ThrowIfNull(cookie);
            cancellationToken.ThrowIfCancellationRequested();
            if (this.cookies.TryGetValue(cookie, out HttpCookieData? existingCookie))
            {
                this.cookies.Remove(existingCookie);
                removed = true;
            }
            if (!string.IsNullOrWhiteSpace(cookie.Uri) || !string.IsNullOrWhiteSpace(cookie.Domain))
            {
                Cookie expiredCookie = new(cookie.Name,
                                           string.Empty,
                                           cookie.Path ?? "/",
                                           cookie.Domain ?? string.Empty)
                {
                    Expired = true
                };
                if (!string.IsNullOrWhiteSpace(cookie.Uri))
                    this.CookieContainer.Add(new Uri(cookie.Uri), expiredCookie);
                else
                    this.CookieContainer.Add(expiredCookie);
                removed = true;
            }
            return Task<bool>.FromResult(removed);
        }

        /// <summary>Returns the live cookie container without creating a detached copy.</summary>
        public CookieContainer BuildCookieContainer()
        {
            return this.CookieContainer;
        }

        #endregion
    }
}
