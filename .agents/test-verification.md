# Local verification — 2026-09-02

## Objective and status

The user authorized test execution after the HTTP pipeline refactor. Restore, solution build, and the selected offline regression suites completed successfully. No source-code correction was needed during this verification.

## Execution

- `git fetch`: completed; HEAD remains 3 commits ahead of origin/main and 0 behind. No pull/merge required.
- `dotnet restore Sources/AMDevIT.Restling`: completed after granting access to the user NuGet configuration/cache outside the sandbox.
- `dotnet build Sources/AMDevIT.Restling --no-restore --verbosity minimal`: passed with 0 warnings and 0 errors. Core and CSV built for net8.0, net9.0, and net10.0; the test project built for net10.0, Debug.
- Tests ran with `dotnet test` on the test project, using `--no-build --no-restore` and explicit class-name filters. No httpbin integration tests were selected.

## Results

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| HttpPipelineCompatibilityTests | 22 | 0 | 0 |
| HttpPipelineEdgeCaseTests | 27 | 0 | 0 |
| ContentCodecTests | 6 | 0 | 0 |
| OwnershipTests | 7 | 0 | 0 |
| MultipartTests | 6 | 0 | 0 |
| SecurityTests (XML) | 6 | 0 | 0 |
| Total | 74 | 0 | 0 |

The first five suites ran together: 68 tests passed, reported duration 511 ms. XML security ran separately: 6 tests passed, reported duration 102 ms. All runtime tests targeted net10.0.

## Reports

- `TestResults/http-pipeline/http-pipeline-regression.trx`
- `TestResults/http-pipeline/xml-security-regression.trx`

Reports and normal build/package outputs are ignored by Git. The source worktree changes from implementation were preserved.

## Remaining scope

- httpbin integration tests were intentionally excluded.
- Runtime tests on net8.0, net9.0, iOS, Android, or MAUI were not performed; successful multi-target compilation is not runtime verification on those platforms.
- A baseline-versus-refactor runtime comparison and coverage measurement were not performed.
- The documented historical untyped POST/PUT-with-headers payload omission remains unchanged and covered by characterization tests.
