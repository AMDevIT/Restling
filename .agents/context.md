# Restling development context
## Objective and status

- Objective: add extensible codecs, explicit resource ownership, and complete MIME multipart support.
- Status: codecs, ownership, buffered multipart, and mixed-replace streaming are implemented; runtime verification is deferred.

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

## Affected files

- Added core codec contracts, registry, implementations, problem model, and integrations.
- Added `AMDevIT.Restling.Csv`, its package README, solution entry, and test reference.
- Added codec models, helper codec, and regression tests.
- Updated the repository and NuGet package READMEs and added `.agents/codecs.md`.
- Added ownership enums, constructor overloads, builder integration, ownership regression tests, documentation, and `.agents/ownership.md`.
- Added multipart request composition, response parsing, models, limits, streaming, regression tests, documentation, and `.agents/multipart.md`.

## Checks performed

- Fetched the remote repository; the working branch required no pull or merge.
- Reviewed CsvHelper 33.1.0 public read/write APIs and RFC 9457 member rules.
- Re-fetched before resuming; the branch remained aligned with `origin/main`.
- Parsed all project XML, checked solution entries, Markdown fences, public method comments, codec registrations, and `git diff --check`.
- Performed static ownership checks for constructor defaults, compatibility aliases, disposal flags, and builder handler behavior.
- Reviewed RFC 2046, RFC 7578, RFC 8710, and the IANA multipart registry before defining multipart scope.
- Did not restore, build, or run tests, as explicitly requested by the user.

## Open issues and recommended next step

- Execute restore, build, and local codec, ownership, and multipart regression tests when authorized.
- Integration tests against httpbin remain separate and were not run.
