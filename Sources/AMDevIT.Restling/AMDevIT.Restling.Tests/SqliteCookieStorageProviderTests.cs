using AMDevIT.Restling.Storage.Relational;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Security.Cryptography;

namespace AMDevIT.Restling.Tests
{
    using Storage;

    [TestClass]
    public sealed class SqliteCookieStorageProviderTests
    {
        #region Methods

        /// <summary>The SQLite provider defaults to a three-second automatic-save interval.</summary>
        [TestMethod]
        public void OptionsDefaultToThreeSecondAutoSave()
        {
            SqliteCookieStorageOptions options = new();

            Assert.IsTrue(options.AutoSave);
            Assert.AreEqual(TimeSpan.FromSeconds(3), options.AutoSaveDelay);
            Assert.AreEqual(SqliteCookieEncryptionMode.None, options.EncryptionMode);
        }

        /// <summary>Plain SQLite storage round-trips cookie columns on explicit save.</summary>
        [TestMethod]
        public async Task PlainStorageRoundTripsCookiesAsync()
        {
            string filePath = CreateStoragePath();

            try
            {
                await using (SqliteCookieStorageProvider source = new(new SqliteCookieStorageOptions
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

                await using SqliteCookieStorageProvider target = new(new SqliteCookieStorageOptions
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
                Assert.AreEqual("plain-value",
                                await ReadScalarAsync<string>(filePath,
                                                              "SELECT value FROM restling_cookies_plain WHERE name = 'session';"));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>Application encryption stores authenticated ciphertext and round-trips with the external protector.</summary>
        [TestMethod]
        public async Task EncryptedStorageRoundTripsWithoutPlaintextRowsAsync()
        {
            string filePath = CreateStoragePath();
            TestDataEncryptionKeyProtector keyProtector = new();

            try
            {
                await using (SqliteCookieStorageProvider source = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    EncryptionMode = SqliteCookieEncryptionMode.Application,
                    FilePath = filePath
                }))
                {
                    source.CookieContainer.Add(new Cookie("session", "sensitive-value", "/", "example.test"));
                    await source.SaveAsync();
                }

                Assert.AreEqual(0L,
                                await ReadScalarAsync<long>(filePath,
                                                            "SELECT COUNT(*) FROM restling_cookies_plain;"));
                Assert.AreEqual(1L,
                                await ReadScalarAsync<long>(filePath,
                                                            "SELECT COUNT(*) FROM restling_cookies_encrypted;"));

                await using SqliteCookieStorageProvider target = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    EncryptionMode = SqliteCookieEncryptionMode.Application,
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

        /// <summary>A database cannot be reopened under a different encryption mode without an explicit migration.</summary>
        [TestMethod]
        public async Task EncryptionModeMismatchIsRejectedAsync()
        {
            string filePath = CreateStoragePath();

            try
            {
                await using (SqliteCookieStorageProvider source = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    FilePath = filePath
                }))
                {
                    await source.SaveAsync();
                }

                await using SqliteCookieStorageProvider target = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = new TestDataEncryptionKeyProtector(),
                    EncryptionMode = SqliteCookieEncryptionMode.Application,
                    FilePath = filePath
                });

                await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => target.LoadAsync());
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>A modified encrypted payload fails authenticated decryption.</summary>
        [TestMethod]
        public async Task TamperedCiphertextIsRejectedAsync()
        {
            string filePath = CreateStoragePath();
            TestDataEncryptionKeyProtector keyProtector = new();

            try
            {
                await using (SqliteCookieStorageProvider source = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    EncryptionMode = SqliteCookieEncryptionMode.Application,
                    FilePath = filePath
                }))
                {
                    source.CookieContainer.Add(new Cookie("session", "sensitive-value", "/", "example.test"));
                    await source.SaveAsync();
                }
                await ExecuteNonQueryAsync(filePath,
                                           "UPDATE restling_cookies_encrypted SET ciphertext = zeroblob(length(ciphertext));");

                await using SqliteCookieStorageProvider target = new(new SqliteCookieStorageOptions
                {
                    AutoSave = false,
                    DataEncryptionKeyProtector = keyProtector,
                    EncryptionMode = SqliteCookieEncryptionMode.Application,
                    FilePath = filePath
                });

                await Assert.ThrowsAsync<CryptographicException>(() => target.LoadAsync());
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
            SqliteCookieStorageProvider provider = new(new SqliteCookieStorageOptions { FilePath = filePath });

            try
            {
                provider.CookieContainer.Add(new Cookie("session", "dispose-value", "/", "example.test"));
                provider.NotifyCookiesChanged();
                await provider.DisposeAsync();

                Assert.AreEqual("dispose-value",
                                await ReadScalarAsync<string>(filePath,
                                                              "SELECT value FROM restling_cookies_plain WHERE name = 'session';"));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>Creates a unique, initially absent SQLite test-storage path.</summary>
        private static string CreateStoragePath()
        {
            return Path.Combine(Path.GetTempPath(), $"restling-cookies-{Guid.NewGuid():N}.db");
        }

        /// <summary>Creates a non-pooled connection so test cleanup can delete the database immediately.</summary>
        private static SqliteConnection CreateConnection(string filePath)
        {
            SqliteConnectionStringBuilder builder = new()
            {
                DataSource = filePath,
                Pooling = false
            };
            return new SqliteConnection(builder.ToString());
        }

        /// <summary>Reads one scalar from a test database.</summary>
        private static async Task<T> ReadScalarAsync<T>(string filePath, string commandText)
        {
            await using SqliteConnection connection = CreateConnection(filePath);
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();

            command.CommandText = commandText;
            object? value = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(value!, typeof(T));
        }

        /// <summary>Executes one mutation against a test database.</summary>
        private static async Task ExecuteNonQueryAsync(string filePath, string commandText)
        {
            await using SqliteConnection connection = CreateConnection(filePath);
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();

            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        #endregion
    }
}
