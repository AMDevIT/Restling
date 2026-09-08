using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Cookies.Storage;
using CoreDataEncryptionKeyProtector = AMDevIT.Restling.Core.Cookies.Storage.Security.IDataEncryptionKeyProtector;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AMDevIT.Restling.Storage.Json
{
    /// <summary>Persists a live cookie jar in versioned plain JSON or an AES-256-GCM envelope.</summary>
    public sealed class JsonCookieStorageProvider : ICookiesStorageProvider, IDisposable, IAsyncDisposable
    {
        #region Const

        private const int DataEncryptionKeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        #endregion

        #region Fields

        private static readonly byte[] additionalAuthenticatedData = Encoding.UTF8.GetBytes("Restling.Storage.Json/v1");
        private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

        private readonly JsonCookieStorageOptions options;
        private readonly CookieContainer cookieContainer = new();
        private readonly SemaphoreSlim storageLock = new(1, 1);
        private readonly object scheduleSync = new();
        private readonly CancellationTokenSource disposalCancellation = new();
        private Task? scheduledSaveTask;
        private byte[]? lastSnapshot;
        private byte[]? dataEncryptionKey;
        private byte[]? protectedDataEncryptionKey;
        private string? dataEncryptionKeyId;
        private bool dirty;
        private bool disposedValue;

        #endregion

        #region Properties

        /// <summary>Gets whether this provider writes authenticated encrypted envelopes.</summary>
        public bool Encrypted => this.options.DataEncryptionKeyProtector != null;

        /// <summary>Gets or sets whether cookie changes schedule automatic persistence.</summary>
        public bool AutoSave { get; set; }

        /// <summary>Gets or sets the interval used to coalesce automatic saves.</summary>
        public TimeSpan AutoSaveDelay { get; }

        /// <summary>Gets the live cookie container used by Restling.</summary>
        public CookieContainer CookieContainer => this.cookieContainer;

        /// <summary>Gets the last background auto-save error, if any.</summary>
        public Exception? LastSaveException { get; private set; }

        #endregion

        #region .ctor

        /// <summary>Creates an advanced JSON cookie storage provider.</summary>
        /// <param name="options">Storage path, automatic-save behavior, and optional DEK protector.</param>
        public JsonCookieStorageProvider(JsonCookieStorageOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.FilePath);
            if (options.AutoSaveDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "AutoSaveDelay must be greater than zero.");
            this.options = options;
            this.AutoSave = options.AutoSave;
            this.AutoSaveDelay = options.AutoSaveDelay;
        }

        #endregion

        #region Methods

        /// <summary>Loads an existing plain document or encrypted envelope into the live cookie jar.</summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            byte[] persistedContent;
            byte[] plainContent;
            JsonCookieDocument? document;

            this.ThrowIfDisposed();
            if (!File.Exists(this.options.FilePath))
                return;

            await this.storageLock.WaitAsync(cancellationToken);
            try
            {
                persistedContent = await File.ReadAllBytesAsync(this.options.FilePath, cancellationToken);
                plainContent = this.Encrypted
                    ? await this.DecryptAsync(persistedContent, cancellationToken)
                    : persistedContent;
                document = JsonSerializer.Deserialize<JsonCookieDocument>(plainContent, serializerOptions);
                if (document == null || document.Version != 1 || document.Format != "Restling.Storage.Json.Plain")
                    throw new InvalidDataException("The JSON cookie storage version is unsupported.");

                foreach (JsonCookieRecord record in document.Cookies.Values)
                {
                    Cookie cookie = record.ToCookie();
                    if (!cookie.Expired)
                        this.CookieContainer.Add(cookie);
                }
                this.lastSnapshot = JsonSerializer.SerializeToUtf8Bytes(document, serializerOptions);
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
                if (this.dataEncryptionKey != null)
                    CryptographicOperations.ZeroMemory(this.dataEncryptionKey);
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

        /// <summary>Writes a changed snapshot through atomic file replacement.</summary>
        private async Task SaveCoreAsync(CancellationToken cancellationToken)
        {
            byte[] plainContent;
            byte[] persistedContent;
            string? directoryPath;
            string temporaryPath;

            await this.storageLock.WaitAsync(cancellationToken);
            try
            {
                plainContent = this.CreateSnapshot();
                if (this.lastSnapshot != null && plainContent.AsSpan().SequenceEqual(this.lastSnapshot))
                    return;
                persistedContent = this.Encrypted
                    ? await this.EncryptAsync(plainContent, cancellationToken)
                    : plainContent;
                directoryPath = Path.GetDirectoryName(Path.GetFullPath(this.options.FilePath));
                if (!string.IsNullOrWhiteSpace(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                temporaryPath = Path.Combine(directoryPath ?? string.Empty, $".{Path.GetFileName(this.options.FilePath)}.{Guid.NewGuid():N}.tmp");
                try
                {
                    await File.WriteAllBytesAsync(temporaryPath, persistedContent, cancellationToken);
                    File.Move(temporaryPath, this.options.FilePath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                this.lastSnapshot = plainContent;
            }
            finally
            {
                this.storageLock.Release();
            }
        }

        /// <summary>Creates a deterministic versioned representation of the current cookie jar.</summary>
        private byte[] CreateSnapshot()
        {
            JsonCookieDocument document = new();

            foreach (Cookie cookie in this.CookieContainer.GetAllCookies())
            {
                JsonCookieRecord record = JsonCookieRecord.FromCookie(cookie);
                if (!cookie.Expired)
                    document.Cookies[record.GetKey()] = record;
            }
            return JsonSerializer.SerializeToUtf8Bytes(document, serializerOptions);
        }

        /// <summary>Encrypts a snapshot with a random nonce and a protected 256-bit DEK.</summary>
        private async Task<byte[]> EncryptAsync(byte[] plainContent, CancellationToken cancellationToken)
        {
            CoreDataEncryptionKeyProtector protector = this.options.DataEncryptionKeyProtector!;
            byte[] ciphertext = new byte[plainContent.Length];
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] tag = new byte[TagSize];
            JsonCookieEnvelope envelope;

            if (this.dataEncryptionKey == null)
            {
                this.dataEncryptionKey = RandomNumberGenerator.GetBytes(DataEncryptionKeySize);
                this.protectedDataEncryptionKey = await protector.ProtectAsync(this.dataEncryptionKey, cancellationToken);
                this.dataEncryptionKeyId = protector.KeyId;
            }
            using (AesGcm aes = new(this.dataEncryptionKey, TagSize))
                aes.Encrypt(nonce, plainContent, ciphertext, tag, additionalAuthenticatedData);
            envelope = new JsonCookieEnvelope
            {
                Ciphertext = Convert.ToBase64String(ciphertext),
                KeyId = this.dataEncryptionKeyId!,
                Nonce = Convert.ToBase64String(nonce),
                ProtectedDataEncryptionKey = Convert.ToBase64String(this.protectedDataEncryptionKey!),
                Tag = Convert.ToBase64String(tag)
            };
            return JsonSerializer.SerializeToUtf8Bytes(envelope, serializerOptions);
        }

        /// <summary>Unprotects the stored DEK and authenticates the encrypted snapshot.</summary>
        private async Task<byte[]> DecryptAsync(byte[] persistedContent, CancellationToken cancellationToken)
        {
            CoreDataEncryptionKeyProtector protector = this.options.DataEncryptionKeyProtector!;
            JsonCookieEnvelope? envelope = JsonSerializer.Deserialize<JsonCookieEnvelope>(persistedContent, serializerOptions);
            byte[] ciphertext;
            byte[] nonce;
            byte[] tag;
            byte[] plainContent;

            if (envelope == null ||
                envelope.Version != 1 ||
                envelope.Format != "Restling.Storage.Json.Encrypted" ||
                envelope.Algorithm != "AES-256-GCM")
                throw new InvalidDataException("The encrypted JSON cookie storage envelope is unsupported.");
            this.protectedDataEncryptionKey = Convert.FromBase64String(envelope.ProtectedDataEncryptionKey);
            this.dataEncryptionKeyId = envelope.KeyId;
            this.dataEncryptionKey = await protector.UnprotectAsync(envelope.KeyId,
                                                                    this.protectedDataEncryptionKey,
                                                                    cancellationToken);
            if (this.dataEncryptionKey.Length != DataEncryptionKeySize)
                throw new CryptographicException("The unprotected data-encryption key must contain 256 bits.");
            ciphertext = Convert.FromBase64String(envelope.Ciphertext);
            nonce = Convert.FromBase64String(envelope.Nonce);
            tag = Convert.FromBase64String(envelope.Tag);
            plainContent = new byte[ciphertext.Length];
            using (AesGcm aes = new(this.dataEncryptionKey, TagSize))
                aes.Decrypt(nonce, ciphertext, tag, plainContent, additionalAuthenticatedData);
            return plainContent;
        }

        /// <summary>Rejects operations after the provider has completed its final flush.</summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(this.disposedValue, this);
        }

        #endregion
    }
}
