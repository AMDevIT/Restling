using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Cookies.Storage;
using AMDevIT.Restling.Core.Cookies.Storage.Security;
using Microsoft.Data.Sqlite;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AMDevIT.Restling.Storage.Relational
{
    /// <summary>Persists cookies in plain SQLite columns or application-encrypted records.</summary>
    public sealed class SqliteCookieStorageProvider : ICookiesStorageProvider, IDisposable, IAsyncDisposable
    {
        #region Const

        private const int DataEncryptionKeySize = 32;
        private const int DerivedKeySize = 32;
        private const int NonceSize = 12;
        private const int SchemaVersion = 1;
        private const int TagSize = 16;

        #endregion

        #region Fields

        private static readonly byte[] encryptionKeyInfo = Encoding.UTF8.GetBytes("Restling.Storage.Sqlite/Encryption/v1");
        private static readonly byte[] indexKeyInfo = Encoding.UTF8.GetBytes("Restling.Storage.Sqlite/Index/v1");
        private static readonly JsonSerializerOptions serializerOptions = new();

        private readonly string filePath;
        private readonly SqliteCookieEncryptionMode encryptionMode;
        private readonly IDataEncryptionKeyProtector? dataEncryptionKeyProtector;
        private readonly CookieContainer cookieContainer = new();
        private readonly SemaphoreSlim storageLock = new(1, 1);
        private readonly object scheduleSync = new();
        private readonly CancellationTokenSource disposalCancellation = new();
        private Task? scheduledSaveTask;
        private byte[]? lastSnapshot;
        private byte[]? dataEncryptionKey;
        private byte[]? derivationSalt;
        private byte[]? protectedDataEncryptionKey;
        private string? dataEncryptionKeyId;
        private bool dirty;
        private bool disposedValue;

        #endregion

        #region Properties

        /// <summary>Gets or sets whether cookie changes schedule automatic persistence.</summary>
        public bool AutoSave { get; set; }

        /// <summary>Gets the interval used to coalesce automatic saves.</summary>
        public TimeSpan AutoSaveDelay { get; }

        /// <summary>Gets the live cookie container used by Restling.</summary>
        public CookieContainer CookieContainer => this.cookieContainer;

        /// <summary>Gets whether cookie rows use application encryption.</summary>
        public bool Encrypted => this.encryptionMode == SqliteCookieEncryptionMode.Application;

        /// <summary>Gets the configured and database-validated encryption mode.</summary>
        public SqliteCookieEncryptionMode EncryptionMode => this.encryptionMode;

        /// <summary>Gets the last background auto-save error, if any.</summary>
        public Exception? LastSaveException { get; private set; }

        #endregion

        #region .ctor

        /// <summary>Creates a plain or application-encrypted SQLite cookie provider.</summary>
        /// <param name="options">Database path, exact encryption mode, auto-save policy, and optional DEK protector.</param>
        public SqliteCookieStorageProvider(SqliteCookieStorageOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.FilePath);
            if (!Enum.IsDefined(options.EncryptionMode))
                throw new ArgumentOutOfRangeException(nameof(options), "EncryptionMode is unsupported.");
            if (options.AutoSaveDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "AutoSaveDelay must be greater than zero.");
            if (options.EncryptionMode == SqliteCookieEncryptionMode.Application && options.DataEncryptionKeyProtector == null)
                throw new ArgumentException("Application encryption requires a data-encryption-key protector.", nameof(options));

            this.filePath = options.FilePath;
            this.encryptionMode = options.EncryptionMode;
            this.dataEncryptionKeyProtector = options.DataEncryptionKeyProtector;
            this.AutoSave = options.AutoSave;
            this.AutoSaveDelay = options.AutoSaveDelay;
        }

        #endregion

        #region Methods

        /// <summary>Loads and validates the configured SQLite storage before its first use.</summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            SqliteConnection connection;

            this.ThrowIfDisposed();
            await this.storageLock.WaitAsync(cancellationToken);
            try
            {
                connection = this.CreateConnection();
                await using (connection)
                {
                    await connection.OpenAsync(cancellationToken);
                    await this.EnsureSchemaAsync(connection, cancellationToken);
                    if (this.EncryptionMode == SqliteCookieEncryptionMode.None)
                        await this.LoadPlainCookiesAsync(connection, cancellationToken);
                    else
                        await this.LoadEncryptedCookiesAsync(connection, cancellationToken);
                }
                this.lastSnapshot = this.CreateSnapshot();
            }
            finally
            {
                this.storageLock.Release();
            }
        }

        /// <summary>Persists the current cookie snapshot immediately.</summary>
        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            this.ThrowIfDisposed();
            lock (this.scheduleSync)
                this.dirty = false;
            return this.SaveCoreAsync(cancellationToken);
        }

        /// <summary>Adds or replaces a cookie in the live jar.</summary>
        public Task<bool> AddCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            Cookie nativeCookie;

            this.ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(cookie);
            cancellationToken.ThrowIfCancellationRequested();
            nativeCookie = new Cookie(cookie.Name, cookie.Value, cookie.Path ?? "/", cookie.Domain ?? string.Empty)
            {
                Secure = cookie.IsSecure ?? false
            };
            if (!string.IsNullOrWhiteSpace(cookie.Uri))
                this.CookieContainer.Add(new Uri(cookie.Uri), nativeCookie);
            else if (!string.IsNullOrWhiteSpace(cookie.Domain))
                this.CookieContainer.Add(nativeCookie);
            else
                throw new ArgumentException("A cookie domain or URI is required.", nameof(cookie));
            this.NotifyCookiesChanged();
            return Task.FromResult(true);
        }

        /// <summary>Expires a cookie in the live jar.</summary>
        public Task<bool> RemoveCookieAsync(HttpCookieData cookie, CancellationToken cancellationToken = default)
        {
            Cookie expiredCookie;

            this.ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(cookie);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(cookie.Domain) && string.IsNullOrWhiteSpace(cookie.Uri))
                return Task.FromResult(false);
            expiredCookie = new Cookie(cookie.Name,
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
            this.NotifyCookiesChanged();
            return Task.FromResult(true);
        }

        /// <summary>Returns the live cookie container without creating a detached copy.</summary>
        public CookieContainer BuildCookieContainer()
        {
            return this.CookieContainer;
        }

        /// <summary>Schedules one coalesced save when automatic persistence is enabled.</summary>
        public void NotifyCookiesChanged()
        {
            if (!this.AutoSave || this.disposedValue)
                return;
            lock (this.scheduleSync)
            {
                this.dirty = true;
                if (this.scheduledSaveTask == null || this.scheduledSaveTask.IsCompleted)
                    this.scheduledSaveTask = this.RunScheduledSaveAsync();
            }
        }

        /// <summary>Flushes pending cookie changes and releases provider resources.</summary>
        public void Dispose()
        {
            this.DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }

        /// <summary>Flushes pending cookie changes asynchronously and releases provider resources.</summary>
        public async ValueTask DisposeAsync()
        {
            Task? pendingSave;

            if (this.disposedValue)
                return;
            lock (this.scheduleSync)
            {
                this.disposedValue = true;
                pendingSave = this.scheduledSaveTask;
            }
            this.disposalCancellation.Cancel();
            if (pendingSave != null)
            {
                try
                {
                    await pendingSave;
                }
                catch (OperationCanceledException)
                {
                }
            }
            try
            {
                if (this.AutoSave)
                {
                    await this.SaveCoreAsync(CancellationToken.None);
                    this.LastSaveException = null;
                }
            }
            catch (Exception exception)
            {
                this.LastSaveException = exception;
                throw;
            }
            finally
            {
                this.disposalCancellation.Dispose();
                this.storageLock.Dispose();
                this.ClearKeys();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>Waits for the coalescing interval and writes one pending snapshot.</summary>
        private async Task RunScheduledSaveAsync()
        {
            try
            {
                await Task.Delay(this.AutoSaveDelay, this.disposalCancellation.Token);
                lock (this.scheduleSync)
                    this.dirty = false;
                await this.SaveCoreAsync(this.disposalCancellation.Token);
                this.LastSaveException = null;
            }
            catch (OperationCanceledException) when (this.disposalCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                this.LastSaveException = exception;
            }
            finally
            {
                lock (this.scheduleSync)
                {
                    this.scheduledSaveTask = null;
                    if (this.dirty && !this.disposedValue)
                        this.scheduledSaveTask = this.RunScheduledSaveAsync();
                }
            }
        }

        /// <summary>Writes a changed cookie snapshot in one SQLite transaction.</summary>
        private async Task SaveCoreAsync(CancellationToken cancellationToken)
        {
            SqliteConnection connection;
            byte[] snapshot;

            await this.storageLock.WaitAsync(cancellationToken);
            try
            {
                snapshot = this.CreateSnapshot();
                if (this.lastSnapshot != null && snapshot.AsSpan().SequenceEqual(this.lastSnapshot))
                    return;
                connection = this.CreateConnection();
                await using (connection)
                {
                    await connection.OpenAsync(cancellationToken);
                    await this.EnsureSchemaAsync(connection, cancellationToken);
                    if (this.EncryptionMode == SqliteCookieEncryptionMode.None)
                        await this.SavePlainCookiesAsync(connection, cancellationToken);
                    else
                        await this.SaveEncryptedCookiesAsync(connection, cancellationToken);
                }
                this.lastSnapshot = snapshot;
            }
            finally
            {
                this.storageLock.Release();
            }
        }

        /// <summary>Creates the connection used for one load or flush operation.</summary>
        private SqliteConnection CreateConnection()
        {
            this.EnsureDatabaseDirectory();
            SqliteConnectionStringBuilder builder = new()
            {
                DataSource = this.filePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };
            return new SqliteConnection(builder.ToString());
        }

        /// <summary>Creates the database directory when the configured path contains one.</summary>
        private void EnsureDatabaseDirectory()
        {
            string? directoryPath = Path.GetDirectoryName(Path.GetFullPath(this.filePath));

            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        /// <summary>Creates storage tables and verifies the persistent mode without fallback.</summary>
        private async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using SqliteCommand command = connection.CreateCommand();
            SqliteDataReader reader;

            command.CommandText = """
                CREATE TABLE IF NOT EXISTS restling_cookie_metadata
                (
                    id INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
                    schema_version INTEGER NOT NULL,
                    encryption_mode INTEGER NOT NULL,
                    key_id TEXT NULL,
                    protected_dek BLOB NULL,
                    derivation_salt BLOB NULL
                );
                CREATE TABLE IF NOT EXISTS restling_cookies_plain
                (
                    domain TEXT NOT NULL COLLATE NOCASE,
                    path TEXT NOT NULL,
                    name TEXT NOT NULL COLLATE NOCASE,
                    value TEXT NOT NULL,
                    expires_binary INTEGER NOT NULL,
                    secure INTEGER NOT NULL,
                    http_only INTEGER NOT NULL,
                    discard INTEGER NOT NULL,
                    version INTEGER NOT NULL,
                    comment TEXT NOT NULL,
                    comment_uri TEXT NULL,
                    port TEXT NOT NULL,
                    PRIMARY KEY (domain, path, name)
                );
                CREATE TABLE IF NOT EXISTS restling_cookies_encrypted
                (
                    key_hash BLOB NOT NULL PRIMARY KEY,
                    nonce BLOB NOT NULL,
                    ciphertext BLOB NOT NULL,
                    tag BLOB NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);

            command.CommandText = "SELECT schema_version, encryption_mode, key_id, protected_dek, derivation_salt FROM restling_cookie_metadata WHERE id = 1;";
            await using (reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    int storedVersion = reader.GetInt32(0);
                    SqliteCookieEncryptionMode storedMode = (SqliteCookieEncryptionMode)reader.GetInt32(1);

                    if (storedVersion != SchemaVersion)
                        throw new InvalidDataException("The SQLite cookie storage schema version is unsupported.");
                    if (storedMode != this.EncryptionMode)
                        throw new InvalidOperationException("The configured encryption mode does not match the SQLite cookie storage. Use an explicit migration.");
                    if (storedMode == SqliteCookieEncryptionMode.Application && this.dataEncryptionKey == null)
                    {
                        this.dataEncryptionKeyId = reader.GetString(2);
                        this.protectedDataEncryptionKey = reader.GetFieldValue<byte[]>(3);
                        this.derivationSalt = reader.GetFieldValue<byte[]>(4);
                    }
                    return;
                }
            }

            if (this.EncryptionMode == SqliteCookieEncryptionMode.Application)
                await this.CreateDataEncryptionKeyAsync(cancellationToken);
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO restling_cookie_metadata (id, schema_version, encryption_mode, key_id, protected_dek, derivation_salt) VALUES (1, $version, $mode, $keyId, $protectedDek, $salt);";
            command.Parameters.AddWithValue("$version", SchemaVersion);
            command.Parameters.AddWithValue("$mode", (int)this.EncryptionMode);
            command.Parameters.AddWithValue("$keyId", (object?)this.dataEncryptionKeyId ?? DBNull.Value);
            command.Parameters.AddWithValue("$protectedDek", (object?)this.protectedDataEncryptionKey ?? DBNull.Value);
            command.Parameters.AddWithValue("$salt", (object?)this.derivationSalt ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>Loads ordinary relational cookie rows.</summary>
        private async Task LoadPlainCookiesAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using SqliteCommand command = connection.CreateCommand();
            SqliteDataReader reader;

            command.CommandText = "SELECT domain, path, name, value, expires_binary, secure, http_only, discard, version, comment, comment_uri, port FROM restling_cookies_plain;";
            await using (reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    SqliteCookieRecord record = ReadPlainRecord(reader);
                    Cookie cookie = record.ToCookie();
                    if (!cookie.Expired)
                        this.CookieContainer.Add(cookie);
                }
            }
        }

        /// <summary>Loads and authenticates application-encrypted cookie rows.</summary>
        private async Task LoadEncryptedCookiesAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using SqliteCommand command = connection.CreateCommand();
            SqliteDataReader reader;
            byte[] encryptionKey;
            byte[] indexKey;

            await this.UnprotectDataEncryptionKeyAsync(cancellationToken);
            encryptionKey = this.DeriveKey(encryptionKeyInfo);
            indexKey = this.DeriveKey(indexKeyInfo);
            try
            {
                command.CommandText = "SELECT key_hash, nonce, ciphertext, tag FROM restling_cookies_encrypted;";
                await using (reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        byte[] keyHash = reader.GetFieldValue<byte[]>(0);
                        byte[] nonce = reader.GetFieldValue<byte[]>(1);
                        byte[] ciphertext = reader.GetFieldValue<byte[]>(2);
                        byte[] tag = reader.GetFieldValue<byte[]>(3);
                        SqliteCookieRecord record = DecryptRecord(keyHash,
                                                                  nonce,
                                                                  ciphertext,
                                                                  tag,
                                                                  encryptionKey,
                                                                  indexKey);
                        Cookie cookie = record.ToCookie();
                        if (!cookie.Expired)
                            this.CookieContainer.Add(cookie);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encryptionKey);
                CryptographicOperations.ZeroMemory(indexKey);
            }
        }

        /// <summary>Replaces plain rows with the current snapshot atomically.</summary>
        private async Task SavePlainCookiesAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();
            using SqliteCommand deleteCommand = connection.CreateCommand();

            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM restling_cookies_plain;";
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            foreach (SqliteCookieRecord record in this.GetCurrentRecords())
                await InsertPlainRecordAsync(connection, transaction, record, cancellationToken);
            transaction.Commit();
        }

        /// <summary>Replaces encrypted rows with freshly nonced authenticated records atomically.</summary>
        private async Task SaveEncryptedCookiesAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();
            using SqliteCommand deleteCommand = connection.CreateCommand();
            byte[] encryptionKey;
            byte[] indexKey;

            await this.UnprotectDataEncryptionKeyAsync(cancellationToken);
            encryptionKey = this.DeriveKey(encryptionKeyInfo);
            indexKey = this.DeriveKey(indexKeyInfo);
            try
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM restling_cookies_encrypted;";
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                foreach (SqliteCookieRecord record in this.GetCurrentRecords())
                    await InsertEncryptedRecordAsync(connection,
                                                     transaction,
                                                     record,
                                                     encryptionKey,
                                                     indexKey,
                                                     cancellationToken);
                transaction.Commit();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encryptionKey);
                CryptographicOperations.ZeroMemory(indexKey);
            }
        }

        /// <summary>Returns current non-expired cookie records in deterministic identity order.</summary>
        private IReadOnlyList<SqliteCookieRecord> GetCurrentRecords()
        {
            return this.CookieContainer.GetAllCookies()
                       .Cast<Cookie>()
                       .Where(cookie => !cookie.Expired)
                       .Select(SqliteCookieRecord.FromCookie)
                       .OrderBy(record => record.GetIdentity(), StringComparer.Ordinal)
                       .ToArray();
        }

        /// <summary>Creates a deterministic snapshot used to skip unchanged transactions.</summary>
        private byte[] CreateSnapshot()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this.GetCurrentRecords(), serializerOptions);
        }

        /// <summary>Generates and protects new application-encryption material.</summary>
        private async Task CreateDataEncryptionKeyAsync(CancellationToken cancellationToken)
        {
            IDataEncryptionKeyProtector protector = this.dataEncryptionKeyProtector!;

            this.dataEncryptionKey = RandomNumberGenerator.GetBytes(DataEncryptionKeySize);
            this.derivationSalt = RandomNumberGenerator.GetBytes(DerivedKeySize);
            this.protectedDataEncryptionKey = await protector.ProtectAsync(this.dataEncryptionKey, cancellationToken);
            this.dataEncryptionKeyId = protector.KeyId;
        }

        /// <summary>Unprotects stored application-encryption material when needed.</summary>
        private async Task UnprotectDataEncryptionKeyAsync(CancellationToken cancellationToken)
        {
            IDataEncryptionKeyProtector protector = this.dataEncryptionKeyProtector!;

            if (this.dataEncryptionKey != null)
                return;
            if (this.dataEncryptionKeyId == null || this.protectedDataEncryptionKey == null || this.derivationSalt == null)
                throw new InvalidDataException("The encrypted SQLite cookie storage has incomplete key metadata.");
            this.dataEncryptionKey = await protector.UnprotectAsync(this.dataEncryptionKeyId,
                                                                    this.protectedDataEncryptionKey,
                                                                    cancellationToken);
            if (this.dataEncryptionKey.Length != DataEncryptionKeySize)
                throw new CryptographicException("The unprotected data-encryption key must contain 256 bits.");
        }

        /// <summary>Derives a purpose-specific 256-bit key from the protected storage DEK.</summary>
        private byte[] DeriveKey(byte[] information)
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256,
                                  this.dataEncryptionKey!,
                                  DerivedKeySize,
                                  this.derivationSalt!,
                                  information);
        }

        /// <summary>Clears unprotected key material before releasing the provider.</summary>
        private void ClearKeys()
        {
            if (this.dataEncryptionKey != null)
                CryptographicOperations.ZeroMemory(this.dataEncryptionKey);
        }

        /// <summary>Rejects operations after the provider has completed its final flush.</summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);
        }

        /// <summary>Reads one ordinary relational cookie row.</summary>
        private static SqliteCookieRecord ReadPlainRecord(SqliteDataReader reader)
        {
            return new SqliteCookieRecord
            {
                Domain = reader.GetString(0),
                Path = reader.GetString(1),
                Name = reader.GetString(2),
                Value = reader.GetString(3),
                ExpiresBinary = reader.GetInt64(4),
                Secure = reader.GetBoolean(5),
                HttpOnly = reader.GetBoolean(6),
                Discard = reader.GetBoolean(7),
                Version = reader.GetInt32(8),
                Comment = reader.GetString(9),
                CommentUri = reader.IsDBNull(10) ? null : reader.GetString(10),
                Port = reader.GetString(11)
            };
        }

        /// <summary>Inserts one ordinary relational cookie row.</summary>
        private static async Task InsertPlainRecordAsync(SqliteConnection connection,
                                                         SqliteTransaction transaction,
                                                         SqliteCookieRecord record,
                                                         CancellationToken cancellationToken)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "INSERT INTO restling_cookies_plain (domain, path, name, value, expires_binary, secure, http_only, discard, version, comment, comment_uri, port) VALUES ($domain, $path, $name, $value, $expires, $secure, $httpOnly, $discard, $version, $comment, $commentUri, $port);";
            command.Parameters.AddWithValue("$domain", record.Domain);
            command.Parameters.AddWithValue("$path", record.Path);
            command.Parameters.AddWithValue("$name", record.Name);
            command.Parameters.AddWithValue("$value", record.Value);
            command.Parameters.AddWithValue("$expires", record.ExpiresBinary);
            command.Parameters.AddWithValue("$secure", record.Secure);
            command.Parameters.AddWithValue("$httpOnly", record.HttpOnly);
            command.Parameters.AddWithValue("$discard", record.Discard);
            command.Parameters.AddWithValue("$version", record.Version);
            command.Parameters.AddWithValue("$comment", record.Comment);
            command.Parameters.AddWithValue("$commentUri", (object?)record.CommentUri ?? DBNull.Value);
            command.Parameters.AddWithValue("$port", record.Port);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>Encrypts and inserts one cookie row using independent encryption and index keys.</summary>
        private static async Task InsertEncryptedRecordAsync(SqliteConnection connection,
                                                             SqliteTransaction transaction,
                                                             SqliteCookieRecord record,
                                                             byte[] encryptionKey,
                                                             byte[] indexKey,
                                                             CancellationToken cancellationToken)
        {
            byte[] identity = Encoding.UTF8.GetBytes(record.GetIdentity());
            byte[] keyHash = HMACSHA256.HashData(indexKey, identity);
            byte[] plainContent = JsonSerializer.SerializeToUtf8Bytes(record, serializerOptions);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] ciphertext = new byte[plainContent.Length];
            byte[] tag = new byte[TagSize];
            byte[] additionalAuthenticatedData = CreateAdditionalAuthenticatedData(keyHash);
            using SqliteCommand command = connection.CreateCommand();

            using (AesGcm aes = new(encryptionKey, TagSize))
                aes.Encrypt(nonce, plainContent, ciphertext, tag, additionalAuthenticatedData);
            CryptographicOperations.ZeroMemory(plainContent);
            CryptographicOperations.ZeroMemory(identity);
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO restling_cookies_encrypted (key_hash, nonce, ciphertext, tag) VALUES ($keyHash, $nonce, $ciphertext, $tag);";
            command.Parameters.AddWithValue("$keyHash", keyHash);
            command.Parameters.AddWithValue("$nonce", nonce);
            command.Parameters.AddWithValue("$ciphertext", ciphertext);
            command.Parameters.AddWithValue("$tag", tag);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>Authenticates and decrypts one encrypted cookie row.</summary>
        private static SqliteCookieRecord DecryptRecord(byte[] keyHash,
                                                        byte[] nonce,
                                                        byte[] ciphertext,
                                                        byte[] tag,
                                                        byte[] encryptionKey,
                                                        byte[] indexKey)
        {
            byte[] plainContent = new byte[ciphertext.Length];
            byte[] additionalAuthenticatedData = CreateAdditionalAuthenticatedData(keyHash);

            try
            {
                using (AesGcm aes = new(encryptionKey, TagSize))
                    aes.Decrypt(nonce, ciphertext, tag, plainContent, additionalAuthenticatedData);
                SqliteCookieRecord record = JsonSerializer.Deserialize<SqliteCookieRecord>(plainContent, serializerOptions)
                    ?? throw new InvalidDataException("The encrypted SQLite cookie record is invalid.");
                byte[] identity = Encoding.UTF8.GetBytes(record.GetIdentity());
                byte[] expectedKeyHash = HMACSHA256.HashData(indexKey, identity);

                CryptographicOperations.ZeroMemory(identity);
                try
                {
                    if (!CryptographicOperations.FixedTimeEquals(keyHash, expectedKeyHash))
                        throw new CryptographicException("The encrypted SQLite cookie identity does not match its blind index.");
                    return record;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(expectedKeyHash);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainContent);
            }
        }

        /// <summary>Binds encrypted cookie content to the schema version and blind index.</summary>
        private static byte[] CreateAdditionalAuthenticatedData(byte[] keyHash)
        {
            byte[] additionalAuthenticatedData = new byte[keyHash.Length + sizeof(int)];

            BinaryPrimitives.WriteInt32BigEndian(additionalAuthenticatedData, SchemaVersion);
            keyHash.CopyTo(additionalAuthenticatedData, sizeof(int));
            return additionalAuthenticatedData;
        }

        #endregion
    }
}
