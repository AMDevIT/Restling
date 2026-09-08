# Restling.Storage.Json

Advanced, versioned JSON cookie persistence for Restling. Automatic persistence is enabled by default and coalesces changes for three seconds before writing one atomic snapshot.

Plain JSON is suitable only when the cookie data is not sensitive. For encrypted storage, provide an `IDataEncryptionKeyProtector` backed by a platform secure store, keychain service, or hardware-protected key. The provider generates a random 256-bit DEK, encrypts cookie data with AES-256-GCM, and stores only the protected DEK beside the ciphertext.

`SaveAsync()` performs an immediate explicit flush. Disposing the provider or an owning Restling context flushes pending automatic changes during orderly shutdown. A process crash can still lose changes made within the pending three-second interval, while atomic replacement preserves the last completed document.
