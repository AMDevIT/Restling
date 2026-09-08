using AMDevIT.Restling.Storage.Json;
using System.Net;

namespace AMDevIT.Restling.Tests
{
    using Storage;

    [TestClass]
    public sealed class JsonCookieStorageProviderTests
    {
        #region Methods

        /// <summary>The advanced provider defaults to a three-second automatic-save interval.</summary>
        [TestMethod]
        public void OptionsDefaultToThreeSecondAutoSave()
        {
            JsonCookieStorageOptions options = new();

            Assert.IsTrue(options.AutoSave);
            Assert.AreEqual(TimeSpan.FromSeconds(3), options.AutoSaveDelay);
        }

        /// <summary>Plain storage round-trips a complete cookie snapshot on explicit save.</summary>
        [TestMethod]
        public async Task PlainStorageRoundTripsCookiesAsync()
        {
            string filePath = CreateStoragePath();

            try
            {
                await using (JsonCookieStorageProvider source = new(new JsonCookieStorageOptions
                {
                    AutoSave = false,
                    FilePath = filePath
                }))
                {
                    source.CookieContainer.Add(new Cookie("session", "plain-value", "/", "example.test")
                    {
                        HttpOnly = true,
                        Secure = true
                    });
                    await source.SaveAsync();
                }

                await using JsonCookieStorageProvider target = new(new JsonCookieStorageOptions
                {
                    AutoSave = false,
                    FilePath = filePath
                });
                await target.LoadAsync();
                Cookie? cookie = target.CookieContainer.GetCookies(new Uri("https://example.test/"))["session"];

                Assert.IsNotNull(cookie);
                Assert.AreEqual("plain-value", cookie.Value);
                Assert.IsTrue(cookie.HttpOnly);
                Assert.IsTrue(cookie.Secure);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>Encrypted storage omits plaintext values and can be loaded with the external key protector.</summary>
        [TestMethod]
        public async Task EncryptedStorageRoundTripsWithoutPlaintextAsync()
        {
            string filePath = CreateStoragePath();
            TestDataEncryptionKeyProtector keyProtector = new();

            try
            {
                await using (JsonCookieStorageProvider source = new(new JsonCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    FilePath = filePath
                }))
                {
                    source.CookieContainer.Add(new Cookie("session", "sensitive-value", "/", "example.test"));
                    await source.SaveAsync();
                }

                string persistedContent = await File.ReadAllTextAsync(filePath);
                Assert.IsFalse(persistedContent.Contains("sensitive-value", StringComparison.Ordinal));

                await using JsonCookieStorageProvider target = new(new JsonCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    FilePath = filePath
                });
                await target.LoadAsync();
                Assert.AreEqual("sensitive-value",
                                target.CookieContainer.GetCookies(new Uri("http://example.test/"))["session"]?.Value);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>Disposal flushes pending automatic changes without waiting for the debounce interval.</summary>
        [TestMethod]
        public async Task DisposeFlushesPendingAutoSaveAsync()
        {
            string filePath = CreateStoragePath();
            JsonCookieStorageProvider provider = new(new JsonCookieStorageOptions { FilePath = filePath });

            try
            {
                provider.CookieContainer.Add(new Cookie("session", "dispose-value", "/", "example.test"));
                provider.NotifyCookiesChanged();
                await provider.DisposeAsync();

                Assert.IsTrue(File.Exists(filePath));
                StringAssert.Contains(await File.ReadAllTextAsync(filePath), "dispose-value");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>Creates a unique, initially absent test-storage path.</summary>
        private static string CreateStoragePath()
        {
            return Path.Combine(Path.GetTempPath(), $"restling-cookies-{Guid.NewGuid():N}.json");
        }

        #endregion
    }
}
