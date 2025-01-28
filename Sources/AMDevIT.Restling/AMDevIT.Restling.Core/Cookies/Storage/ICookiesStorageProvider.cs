using System.Net;

namespace AMDevIT.Restling.Core.Cookies.Storage
{
    internal interface ICookiesStorageProvider
    {
        #region Properties
        
        bool Encrypted
        {
            get;
        }

        #endregion

        #region Methods

        public Task LoadAsync(CancellationToken cancellationToken = default);
        public Task SaveAsync(CancellationToken cancellationToken = default);

        
        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default);
        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default);

        public CookieContainer BuildCookieContainer();


        #endregion
    }
}
