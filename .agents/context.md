# Restling development context
## Objective and status

- Objective: add extensible codecs, explicit resource ownership, complete MIME multipart support, and centralized HTTP execution with historical-behavior regression tests.
- Status: implementations, static review, and authorized local verification are complete. Solution build passed without warnings/errors; 74 offline tests passed on net10.0.

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
- The legacy untyped POST/PUT header overloads' bodyless behavior is documented and tested, not silently corrected in this refactor.

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

## Open issues and recommended next step

- Local regression tests are green; runtime verification on other target frameworks/platforms and coverage/baseline comparison remain outside this run.
- Integration tests against httpbin remain separate and were not run.
- Evaluate a separately approved fix for the historical untyped POST/PUT-with-headers payload omission.
