# Content codecs context
## Objective and status

- Objective: add extensible content codecs with backward-compatible JSON, XML, text, and binary/raw defaults; add opt-in CSV and Problem Details.
- Status: implemented, awaiting restore/build/test verification.

## Decisions made

- `ContentCodecRegistry` is immutable and first-match-wins; custom codecs are prepended.
- Every `HttpClientContext` receives the four backward-compatible default codecs.
- Non-JSON request serialization is explicit through `RestRequest<T>.UseContentCodec` to preserve legacy media-type relabeling.
- Problem Details validates RFC 9457 member types and exposes extension members without dereferencing URI references.
- CSV is isolated in `Restling.Csv` so CsvHelper is not a dependency of the core package.

## Affected files

- Added core codec contracts, registry, implementations, problem model, and integrations.
- Added `AMDevIT.Restling.Csv`, its package README, solution entry, and test reference.
- Added codec models, helper codec, and regression tests.
- Updated the repository and NuGet package READMEs and added `.agents/codecs.md`.

## Checks performed

- Fetched the remote repository; the working branch required no pull or merge.
- Reviewed CsvHelper 33.1.0 public read/write APIs and RFC 9457 member rules.
- Re-fetched before resuming; the branch remained aligned with `origin/main`.
- Parsed all project XML, checked solution entries, Markdown fences, public method comments, codec registrations, and `git diff --check`.
- Did not restore, build, or run tests, as explicitly requested by the user.

## Open issues and recommended next step

- Execute restore, build, and local codec regression tests when authorized.
- Integration tests against httpbin remain separate and were not run.
