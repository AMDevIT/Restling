# Cookie storage persistence

## Objective and status

Evolve cookie storage into a public provider contract, retain a deliberately basic in-memory/default implementation, and add extensible JSON and relational packages. The Core integration, advanced JSON implementation, relational project placeholder, documentation, and regression sources are complete. Restore, build, and test execution remain pending explicit user authorization.

## Decisions

- `ICookiesStorageProvider` is public and exposes the exact live `CookieContainer` bound to Restling transports.
- `CookieStorageProvider` remains in Core. Its parameterless form is in-memory; its existing path-based form retains basic JSON load/save compatibility. Basic persistence is explicit only and is documented as unsuitable for secure storage.
- `HttpClientContextBuilder.AddCookieStorageProvider` transfers provider ownership to the generated context. A later `AddCookieContainer` selection replaces the provider with a basic provider around that container.
- Attached storage is loaded once before the first request. After each received HTTP response the pipeline emits a change notification; it does not call `SaveAsync` directly.
- `Restling.Storage.Json` supplies `JsonCookieStorageProvider`. `AutoSave` defaults to true and notifications are coalesced for three seconds without indefinitely postponing writes under sustained traffic.
- An explicit `SaveAsync` writes immediately. Orderly disposal cancels any pending delay and performs a final snapshot; forced process termination can still lose the final pending interval.
- Unchanged snapshots are not rewritten. JSON replacement uses a temporary file in the destination directory followed by overwrite/rename.
- Plain documents are versioned dictionaries keyed by the cookie's domain/path/name identity and retain the public .NET cookie metadata.
- Encrypted documents use AES-256-GCM with a fresh nonce per write and authenticated format metadata. A random 256-bit DEK is stored only after protection through an application-supplied `IDataEncryptionKeyProtector`; the protecting KEK remains outside the JSON file.
- `Restling.Storage.Relational` now supplies a SQLite provider with explicit plain and application-encrypted modes. See `.agents/sqlite-cookie-storage.md` for its schema, cryptography, lifecycle, and remaining verification.

## Affected files

- Core cookie storage interface/provider, context builder/interface, context lifecycle, HTTP pipeline, and RestlingClient pipeline construction.
- Added `AMDevIT.Restling.Storage.Json` implementation, package metadata, and README.
- Added `AMDevIT.Restling.Storage.Relational` package and SQLite implementation; the later implementation step is recorded separately.
- Updated solution and test project references.
- Added JSON plain/encrypted/disposal test sources, a test DEK protector, and a pipeline/provider notification regression.
- Updated repository and package documentation.

## Checks performed

- Preliminary fetch completed; `Task-CookiePersistence` started clean at the same commit as `origin/main`, so no pull or merge was required.
- `git diff --check` passes; only expected line-ending normalization warnings are reported.
- Both new project files parse as XML.
- `dotnet sln ... list` recognizes Core, CSV, JSON storage, relational storage, and tests.
- Restore, compilation, package validation, and runtime tests have not been run because the user has not authorized them yet.

## Open issues and recommended next step

- Obtain authorization, then restore and build the complete solution and run the new cookie-storage tests plus the existing cookie, pipeline, proxy, and ownership regressions.
- Direct external mutations of `CookieContainer` have no .NET change event. They are captured on the next pipeline notification, explicit save, or orderly dispose, not immediately.
- Background auto-save failures are exposed through `LastSaveException`; a later policy may add structured logging or an error event.
- Execute the pending SQLite and existing cookie-storage verification after explicit authorization.
