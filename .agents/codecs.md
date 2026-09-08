# Content codecs implementation

- Immutable first-match-wins registry.
- JSON, XML, text and binary/raw defaults.
- Opt-in RFC 9457 Problem Details and optional CsvHelper-based `Restling.Csv`.
- Existing request payloads stay JSON unless `UseContentCodec` is enabled.
- Restore/build/tests were initially deferred; on 2026-09-02 the authorized build and all 6 codec tests passed. See `test-verification.md`.
