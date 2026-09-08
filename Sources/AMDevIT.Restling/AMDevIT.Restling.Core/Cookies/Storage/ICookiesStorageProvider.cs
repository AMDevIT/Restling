using System.Net;

namespace AMDevIT.Restling.Core.Cookies.Storage
{
    /// <summary>Provides the live cookie jar used by Restling and optional persistence operations.</summary>
    public interface ICookiesStorageProvider
    {
        #region Properties
        
        /// <summary>Gets whether persisted cookie data is encrypted.</summary>
        bool Encrypted { get; }

        /// <summary>Gets the cookie container shared with the HTTP transport.</summary>
        CookieContainer CookieContainer { get; }

        #endregion

        #region Methods

        /// <summary>Loads persisted cookies into the live cookie container.</summary>
        public Task LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>Persists the current live cookie container when explicitly requested.</summary>
        public Task SaveAsync(CancellationToken cancellationToken = default);

        
        /// <summary>Adds or replaces a cookie in the live jar.</summary>
        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default);

        /// <summary>Removes a cookie from the live jar.</summary>
        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default);

        /// <summary>Returns the live cookie container used by this provider.</summary>
        public CookieContainer BuildCookieContainer();

        /// <summary>Notifies the provider that the shared cookie container may have changed.</summary>
        /// <remarks>Basic providers may ignore this notification. Advanced providers can use it for opt-in automatic persistence.</remarks>
        public void NotifyCookiesChanged()
        {
        }


        #endregion
    }
}
