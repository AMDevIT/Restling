# Restling development context
## Objective and status

- Objective: add extensible codecs, explicit resource ownership, complete MIME multipart support, centralized HTTP execution, pluggable cookie persistence, and status-based complex response data mapping.
- Status: Status-based response data mapping is implemented, documented, and verified with 18 targeted and 202 relevant regression cases passing. The full solution builds with 0 warnings/errors. Four separate SQLite storage tests expose a pre-existing Windows file-lock cleanup issue.

## Decisions made

- `ContentCodecRegistry` is immutable and first-match-wins; custom codecs are prepended.
- Every `HttpClientContext` receives the four backward-compatible codecs plus the multipart reader.
- Non-JSON request serialization is explicit through `RestRequest<T>.UseContentCodec` to preserve legacy media-type relabeling.
- Problem Details validates RFC 9457 member types and exposes extension members without dereferencing URI references.
- CSV is isolated in `Restling.Csv` so CsvHelper is not a dependency of the core package.
- Clients own contexts they create and borrow externally supplied contexts by default.
- `DisposeContext` remains a compatibility alias for the explicit `ContextOwnership` enum.
- Context ownership uses flags so `HttpClient` and `HttpMessageHandler` disposal can be selected independently.
- Builder-created contexts own their `HttpClient`; supplied handlers are borrowed by default, while internally created handlers are owned.
- Legacy context and handler overloads remain available.
- Multipart writing accepts any MIME subtype and creates each part per execution.
- Buffered multipart responses preserve MIME structure and decode parts through the configured codecs.
- `multipart/x-mixed-replace` uses a separate incremental API rather than the buffered response parser.
- Internal HTTP pipeline centralizes sending, timing, decoding, error results, logging, and response lifetime without taking ownership of shared transport resources.
- Historical serializer precedence, null-payload handling, direct HttpClient version defaults, and result-versus-exception differences remain explicit at the pipeline boundary.
- After separate user approval, the untyped POST/PUT header overloads now send their payload and return an untyped result through the centralized pipeline. Other serializer/overload contracts remain unchanged.
- After explicit approval, cookie binding is centralized for directly supplied/configured native handlers. Explicit containers take precedence; otherwise the handler's existing jar and cookie policy are retained. Redirect and ownership settings remain unchanged.
- AddProxy(string proxyUri, bool allowAutoRedirect) configures directly supported native handlers, enables the context's explicit proxy, and selects HTTP redirect behavior while preserving cookies/ownership. It supports handler registration in either order; later ConfigureHandler changes remain authoritative for that handler. Custom/delegating default handlers are rejected by AddProxy; request-level alternatives use an explicit factory when needed.
- RestRequest.ProxyOptions now selects Default, Direct, or Custom routing per request. Alternative transports are cached by immutable proxy/redirect selection, share the context CookieContainer, copy HttpClient defaults, and are owned by the context. Default builders supply a factory; externally supplied/configured handlers require AddRequestHandlerFactory rather than unsafe cloning.
- All 16 direct GET/POST/PUT/DELETE variants expose RequestProxyOptions before the final CancellationToken. Serializer parameters precede it to avoid ambiguity with existing positional null calls; IRestlingClient default bodies preserve compatibility for external implementations.
- A request-specific transport that fails with `HttpRequestError.ResponseEnded` is evicted and disposed only if it is still the cached instance. The default client remains untouched, and the caller's next retry creates a fresh native handler, connection pool, and SOCKS tunnel.
- `ICookiesStorageProvider` is public and owns the live cookie jar. Core's `CookieStorageProvider` is primarily in-memory with explicit basic JSON persistence; Restling never automatically calls its `SaveAsync`.
- An attached provider loads once before the first request and receives change notifications after HTTP responses. `Restling.Storage.Json` opts into those notifications by default, coalesces them for three seconds, skips unchanged snapshots, writes atomically, and flushes during orderly disposal.
- Advanced encrypted JSON uses AES-256-GCM and a random DEK protected by an application-supplied external key protector. The JSON file never contains an unprotected DEK.
- `SqliteCookieStorageProvider` supports an explicit plain relational mode and an application-encrypted mode in one provider. The database persists its exact mode and refuses implicit fallback or migration.
- Encrypted SQLite rows use separately derived AES-256-GCM and HMAC-SHA-256 keys. The shared DEK-protector contract now lives in Core; the JSON namespace retains an obsolete compatibility interface.
- Status-response models require no shared marker interface. `RestRequest.ResponseMappings` selects arbitrary codec-supported types by exact status, ordered status pattern, or fallback.
- Exact response mappings override ranges; overlapping ranges are first-match-wins; re-registering an exact status replaces it. A matched response is decoded once into `MappedData`, while unmapped requests retain the existing `Data` behavior.
- Mapped response failures are exposed separately through `MappedDataException` without losing HTTP metadata, raw content, or optional Problem Details.

## Affected files

- Added core codec contracts, registry, implementations, problem model, and integrations.
- Added `AMDevIT.Restling.Csv`, its package README, solution entry, and test reference.
- Added codec models, helper codec, and regression tests.
- Updated the repository and NuGet package READMEs and added `.agents/codecs.md`.
- Added ownership enums, constructor overloads, builder integration, ownership regression tests, documentation, and `.agents/ownership.md`.
- Added multipart request composition, response parsing, models, limits, streaming, regression tests, documentation, and `.agents/multipart.md`.
- Added internal pipeline/streaming lease, integrated all client send paths, added 49 deterministic pipeline regression cases and test helpers, and documented the step in `.agents/http-pipeline.md`.
- Corrected the existing multipart tests' HttpMethod namespace alias.
- Recorded authorized restore/build/test results and remaining verification scope in `.agents/test-verification.md`.
- Updated POST/PUT regression tests, added loopback cookie tests/helper, and recorded the follow-up in `.agents/post-put-cookies.md`.
- Corrected HttpClientContextBuilder cookie binding, added 18 CookieBuilderTests cases, and recorded completion in `.agents/cookie-builder.md`.
- Added AddProxy to the builder/interface, 43 ProxyBuilderTests cases, proxy sections in both READMEs, and `.agents/proxy-builder.md`.
- Added request routing models/pool, integrated transport selection into the centralized buffered/streaming pipeline, added 16 RequestProxyOverrideTests cases, documented usage, and recorded `.agents/request-proxy.md`.
- Added direct proxy overloads to RestlingClient/IRestlingClient and an aggregate test that invokes all 16 signatures and verifies CancellationToken is last.
- Added targeted `ResponseEnded` recovery across the request transport pool and HTTP pipeline, plus a deterministic loopback regression. See `.agents/response-ended-recovery.md`.
- Added public cookie storage integration, `Restling.Storage.Json`, the relational storage project placeholder, documentation, and persistence regression sources. See `.agents/cookie-storage-persistence.md`.
- Implemented the relational SQLite provider, package documentation, and regression sources. See `.agents/sqlite-cookie-storage.md`.
- Added status-pattern and mapping contracts, integrated them into buffered response parsing, added 18 deterministic cases, and documented the API. See `.agents/status-response-mapping.md`.

## Checks performed

- Fetched the remote repository; the working branch required no pull or merge.
- Reviewed CsvHelper 33.1.0 public read/write APIs and RFC 9457 member rules.
- Re-fetched before resuming; the branch remained aligned with `origin/main`.
- Parsed all project XML, checked solution entries, Markdown fences, public method comments, codec registrations, and `git diff --check`.
- Performed static ownership checks for constructor defaults, compatibility aliases, disposal flags, and builder handler behavior.
- Reviewed RFC 2046, RFC 7578, RFC 8710, and the IANA multipart registry before defining multipart scope.
- Restore/build/test execution was initially deferred at the user's request, then explicitly authorized and completed on 2026-09-02.
- Fetched again for pipeline completion: HEAD is 3 commits ahead of origin/main, 0 behind; no pull/merge needed.
- Statically compared pipeline behavior against the pre-refactor implementation, verified centralized send call sites, and checked the diff for whitespace errors. These checks do not establish that the new tests pass.
- Authorized verification: restore passed; solution build passed for Core/CSV net8.0, net9.0, net10.0 and tests net10.0 with 0 warnings/errors.
- Runtime verification: 49 pipeline, 6 codec, 7 ownership, 6 multipart, and 6 XML security cases passed (74 total; 0 failed/skipped) on net10.0. TRX reports are under `TestResults/http-pipeline/`.
- Follow-up verification: reproduced the POST/PUT bug with 4 failing tests before fixing it. Latest run has 57 pipeline + 25 existing codec/ownership/multipart/security cases passing; 10/18 cookie cases pass and 8 expose the builder issue. Reports are under `TestResults/post-put-cookies/`.
- Cookie-builder completion: the preceding eight cookie failures are resolved. Targeted cookie tests: 36/36 passed; full selected suite: 118/118 passed, 0 failed/skipped. Solution build passed with 0 warnings/errors. Reports are under `TestResults/cookie-builder/`.
- Proxy completion: fetched Task-NewCodecs (aligned with upstream), restored and built successfully with 0 warnings/errors. All 161 selected local tests passed (118 existing + 43 proxy), including actual loopback proxy redirects/cookie persistence with both native handlers. Reports are under `TestResults/proxy/`; git diff --check passed.
- Per-request proxy completion: after the user pulled two upstream commits, fetch confirmed alignment. Restore succeeded; the final build passed across Core/CSV net8/net9/net10 and tests net10 with 0 warnings/errors. An intermediate test-triggered build emitted one generated MSTest CS8892 warning, absent from the final build. Targeted proxy tests passed 59/59 and the selected regression passed 177/177. Reports are under `TestResults/request-proxy/`; git diff --check passed.
- Direct-overload follow-up: the 60 proxy tests and all 178 selected regression tests passed. Every new signature was exercised through IRestlingClient; reports are in TestResults/request-proxy/.
- Follow-up multi-target build passed with 0 errors; the only warning was the previously observed CS8892 in generated MSTest entry-point code.
- `ResponseEnded` recovery follow-up: fetched the remote and confirmed the clean branch was aligned with its upstream before editing. The resulting targeted diff was inspected. No restore, build, or tests were run at the user's request.
- Cookie persistence step: fetch confirmed `Task-CookiePersistence` started aligned with `origin/main`; new project XML and solution membership were checked, and `git diff --check` passed. Restore/build/tests remain unauthorized and were not run.
- SQLite persistence step: fetched before editing; project XML parsing and `git diff --check` pass. Restore/build/tests remain unauthorized and were not run.
- Status-response completion: restore passed and the full multi-target solution build completed with 0 warnings/errors. The targeted suite passed 18/18 and the relevant local regression passed 202/202 on net10.0. A broader 208-case run passed 204 and reproduced four unrelated SQLite Windows file-lock failures; the isolated SQLite suite passed 2/6 with the same failures. Reports are under `TestResults/status-response/`.

## Open issues and recommended next step

- No known failures remain in the selected non-SQLite local suites. Opaque custom/delegating-handler cookie processing remains the caller's responsibility; only directly supported native handlers are bound automatically.
- Runtime verification on other target frameworks/platforms and coverage/baseline comparison remain outside this run.
- Integration tests against httpbin remain separate and were not run.
- `ResponseEndedInvalidatesAlternativeTransport` and the related proxy suites pass in the latest relevant regression run.
- POST/PUT payload omission is fixed; general serializer-precedence normalization remains separate.
- Per-request direct/custom proxy selection and convenience overloads are implemented. HTTPS CONNECT/TLS proxy, SOCKS handshakes, real proxy authentication exchanges, other runtime/platform executions, and bounded cache eviction remain untested/out of scope.
- Cookie persistence compiles; JSON and core cookie regressions pass. Direct external `CookieContainer` changes are detected at the next request notification, explicit save, or orderly dispose because `CookieContainer` exposes no mutation event.
- SQLite mode migration, full-file encryption, rollback protection, key rotation, and multi-process coordination remain outside the current relational provider step.
- Status-based response mapping has no known failure in the relevant local suites. External httpbin and non-net10 runtime execution remain outside this run.
- Four SQLite storage regressions cannot delete their temporary database on Windows because the file remains open; investigate separately from issue #37.
