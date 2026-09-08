# SQLite cookie storage

## Objective and status

Implement the relational cookie-storage step with one SQLite provider supporting explicit plain and application-encrypted modes. Source, documentation, and regression tests are complete; restore, build, and test execution remain pending explicit user authorization.

## Decisions

- `SqliteCookieStorageProvider` implements the public `ICookiesStorageProvider` contract in `Restling.Storage.Relational`.
- `SqliteCookieEncryptionMode.None` uses ordinary relational cookie columns. `Application` stores an HMAC-SHA-256 blind index plus an AES-256-GCM authenticated payload.
- The schema records its version and encryption mode. A configuration mismatch is rejected and requires an explicit future migration; there is no silent fallback or conversion.
- A random 256-bit DEK is protected through the shared Core `IDataEncryptionKeyProtector`. HKDF-SHA-256 derives separate encryption and blind-index keys from the DEK and a random salt.
- Every encrypted row uses a fresh 96-bit nonce and 128-bit authentication tag. Authenticated data binds the ciphertext to schema version and blind index; loading also recomputes and verifies the blind index.
- Automatic persistence defaults to a coalesced three-second flush. Explicit `SaveAsync` writes immediately, unchanged snapshots are skipped, and orderly disposal performs a final flush.
- Writes replace the selected representation in one transaction. SQLite connection pooling is disabled so short-lived provider operations do not retain database file handles.
- Application encryption is documented as record-level protection, not whole-database encryption. Schema, row counts, approximate sizes, file structure, updates, deletion, and rollback remain observable.
- The DEK-protector contract moved to Core for reuse. The original JSON-namespace interface remains as an obsolete compatibility interface inheriting the shared contract.

## Affected files

- Added SQLite mode, options, record, and provider sources in `AMDevIT.Restling.Storage.Relational`.
- Added the `Microsoft.Data.Sqlite` dependency and updated relational package metadata and README.
- Added the shared Core DEK-protector contract and updated JSON storage to consume it.
- Added relational project test reference and SQLite regression test sources.
- Updated repository and package documentation with configuration and security boundaries.

## Checks performed

- The repository was fetched before implementation; no pull or merge was required.
- Relational and test project files parse as XML.
- `git diff --check` passes; only expected line-ending normalization warnings are reported.
- Static inspection confirms that encrypted rows contain only blind-index, nonce, ciphertext, and tag columns, and that database mode mismatches fail before loading records.
- Restore, compilation, package validation, and runtime tests have not been run because the user has not authorized them yet.

## Open issues and recommended next step

- Obtain authorization, then restore and build the solution and run the SQLite, JSON, cookie, pipeline, and ownership regression suites.
- Explicit migration between plain and application-encrypted databases is intentionally outside this step.
- Rollback protection, full-file encryption, multi-process coordination, key rotation, and online schema migration are future concerns.
