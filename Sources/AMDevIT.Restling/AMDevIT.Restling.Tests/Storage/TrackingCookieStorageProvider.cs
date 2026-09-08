using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Cookies.Storage;
using System.Net;

namespace AMDevIT.Restling.Tests.Storage
{
    internal sealed class TrackingCookieStorageProvider : ICookiesStorageProvider
    {
        #region Fields

        private int loadCount;
        private int notificationCount;

        #endregion

        #region Properties

        public CookieContainer CookieContainer { get; } = new();
        public bool Encrypted => false;
        public int LoadCount => this.loadCount;
        public int NotificationCount => this.notificationCount;

        #endregion

        #region Methods

        /// <summary>Records a pipeline load.</summary>
        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref this.loadCount);
            return Task.CompletedTask;
        }

        /// <summary>Completes the unused test save operation.</summary>
        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>Is not required by this notification-only test double.</summary>
        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <summary>Is not required by this notification-only test double.</summary>
        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <summary>Returns the tracked live jar.</summary>
        public CookieContainer BuildCookieContainer()
        {
            return this.CookieContainer;
        }

        /// <summary>Records a pipeline change notification.</summary>
        public void NotifyCookiesChanged()
        {
            Interlocked.Increment(ref this.notificationCount);
        }

        #endregion
    }
}
