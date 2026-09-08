# Restling.Storage.Relational

`Restling.Storage.Relational` provides SQLite cookie persistence through a single `SqliteCookieStorageProvider`. The provider supports two explicit modes:

- `SqliteCookieEncryptionMode.None` stores cookie properties in ordinary relational columns.
- `SqliteCookieEncryptionMode.Application` stores only an HMAC-SHA-256 blind index and an AES-256-GCM authenticated payload for each cookie.

The database records its mode and schema version. Opening it with a different mode fails instead of silently reading or rewriting the other representation; changing mode requires an explicit migration.

```csharp
using AMDevIT.Restling.Core.Cookies.Storage.Security;
using AMDevIT.Restling.Storage.Relational;

SqliteCookieStorageOptions options = new()
{
    FilePath = databasePath,
    EncryptionMode = SqliteCookieEncryptionMode.Application,
    DataEncryptionKeyProtector = keyProtector
};
SqliteCookieStorageProvider storage = new(options);
```

Automatic persistence is enabled by default. Change notifications are coalesced into one flush after three seconds; `SaveAsync()` flushes immediately, and orderly disposal performs a final flush. Set `AutoSave = false` when the application must control every write explicitly.

Application encryption generates a random 256-bit data-encryption key (DEK). Separate encryption and blind-index keys are derived from it, while the DEK itself is persisted only after protection by the supplied `IDataEncryptionKeyProtector`. Keep the protecting key outside the database in a platform keychain, secure store, or hardware-backed facility.

This mode protects cookie names, values, domains, paths, and metadata against offline disclosure and detects row tampering. It is not whole-database encryption: the SQLite schema, metadata, number and approximate size of records, update times, and database file structure remain observable. It also does not prevent rollback or deletion of the database. Protect the file with operating-system permissions and use full-database encryption when those leaks are unacceptable.
