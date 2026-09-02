# Restling development context
## Objective and status

- Objective: add extensible codecs, explicit resource ownership, complete MIME multipart support, and centralized HTTP execution with historical-behavior regression tests.
- Status: POST/PUT payload and cookie-builder corrections plus explicit AddProxy configuration are implemented and verified. Latest solution build passed without warnings/errors; all 161 selected local tests passed on net10.0, including 36 cookie and 43 proxy cases.

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
- AddProxy(string proxyUri, bool allowAutoRedirect) configures directly supported native handlers, enables the explicit proxy, and selects HTTP redirect behavior while preserving cookies/ownership. It supports handler registration in either order; later ConfigureHandler changes remain authoritative for that handler. Custom/delegating handlers are rejected; per-request proxy overrides are deferred.

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

## Open issues and recommended next step

- No known failures remain in the selected local suites. Opaque custom/delegating-handler cookie processing remains the caller's responsibility; only directly supported native handlers are bound automatically.
- Runtime verification on other target frameworks/platforms and coverage/baseline comparison remain outside this run.
- Integration tests against httpbin remain separate and were not run.
- POST/PUT payload omission is fixed; general serializer-precedence normalization remains separate.
- Per-request direct/custom proxy selection is not implemented. HTTPS CONNECT/TLS proxy, SOCKS handshakes, and proxy authentication exchanges remain untested; supported-scheme and credential-setting tests only verify configuration.
