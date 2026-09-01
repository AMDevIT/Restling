# Restling development context
## Objective and status

- Objective: add extensible codecs and make client, context, HTTP client, and handler ownership explicit.
- Status: codecs and explicit ownership are implemented; restore/build/test verification is deferred.

## Decisions made

- `ContentCodecRegistry` is immutable and first-match-wins; custom codecs are prepended.
- Every `HttpClientContext` receives the four backward-compatible default codecs.
- Non-JSON request serialization is explicit through `RestRequest<T>.UseContentCodec` to preserve legacy media-type relabeling.
- Problem Details validates RFC 9457 member types and exposes extension members without dereferencing URI references.
- CSV is isolated in `Restling.Csv` so CsvHelper is not a dependency of the core package.
- Clients own contexts they create and borrow externally supplied contexts by default.
- `DisposeContext` remains a compatibility alias for the explicit `ContextOwnership` enum.
- Context ownership uses flags so `HttpClient` and `HttpMessageHandler` disposal can be selected independently.
- Builder-created contexts own their `HttpClient`; supplied handlers are borrowed by default, while internally created handlers are owned.
- Legacy context and handler overloads remain available.

## Affected files

- Added core codec contracts, registry, implementations, problem model, and integrations.
- Added `AMDevIT.Restling.Csv`, its package README, solution entry, and test reference.
- Added codec models, helper codec, and regression tests.
- Updated the repository and NuGet package READMEs and added `.agents/codecs.md`.
- Added ownership enums, constructor overloads, builder integration, ownership regression tests, documentation, and `.agents/ownership.md`.

## Checks performed

- Fetched the remote repository; the working branch required no pull or merge.
- Reviewed CsvHelper 33.1.0 public read/write APIs and RFC 9457 member rules.
- Re-fetched before resuming; the branch remained aligned with `origin/main`.
- Parsed all project XML, checked solution entries, Markdown fences, public method comments, codec registrations, and `git diff --check`.
- Performed static ownership checks for constructor defaults, compatibility aliases, disposal flags, and builder handler behavior.
- Did not restore, build, or run tests, as explicitly requested by the user.

## Open issues and recommended next step

- Execute restore, build, and local codec and ownership regression tests when authorized.
- Integration tests against httpbin remain separate and were not run.
