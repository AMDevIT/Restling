# Centralized HTTP pipeline

## Objective and status

Centralize transport execution while preserving the existing public API and observable request/result contracts. Implementation, static review, and authorized local verification are complete. All 49 new pipeline cases passed on net10.0; see `test-verification.md` for the full 74-test run.

## Architecture

- Internal `HttpExecutionPipeline` is the only component calling `HttpClient.SendAsync`.
- Buffered requests share timing, send-error conversion, parser/codec configuration, logging, and response disposal.
- Elapsed time covers sending and buffering, excluding request serialization and response decoding.
- Direct convenience methods use a request factory so preparation failures still become results with zero elapsed time. Explicit request APIs retain thrown preparation failures.
- `HttpResponseLease` owns streaming responses. Mixed-replace uses `ResponseHeadersRead`, keeps exception propagation, and releases its response when enumeration completes or stops early.
- Buffered responses are now released in `finally`, including when a decoder throws. The pipeline does not own or dispose the shared HttpClient/context.
- Log messages are standardized without adding request URI parameters to pipeline logs.

## Compatibility decisions

- Direct typed GET/DELETE/POST/PUT response decoding uses the explicit serializer before the client default.
- Direct POST/PUT request serialization uses the explicit serializer or automatic detection, not the client default.
- Header/explicit-request paths keep their historical default-serializer override during execution, after payload construction.
- Raw/form requests keep their own serializer selection and do not acquire the client default or mutate the request's serializer setting.
- Null direct POST/PUT payloads retain empty UTF-8 text content; explicit payload-request behavior is unchanged.
- Direct convenience methods retain HttpClient default request version/version policy; explicitly built request paths are unchanged.
- Buffered send errors and cancellation remain unsuccessful results; streaming failures continue to throw.
- Existing JSON default-on-decode-error and status-dependent XML errors are retained.
- Historical anomaly deliberately preserved: untyped POST/PUT overloads with request headers dispatch to bodyless execution and return a typed result through the base result type. Correcting that behavior requires a separate approved behavioral change.

## Files and tests

- `AMDevIT.Restling.Core/RestlingClient.cs`
- `AMDevIT.Restling.Core/Network/Pipeline/HttpExecutionPipeline.cs`
- `AMDevIT.Restling.Core/Network/Pipeline/HttpResponseLease.cs`
- `AMDevIT.Restling.Tests/HttpPipelineCompatibilityTests.cs`
- `AMDevIT.Restling.Tests/HttpPipelineEdgeCaseTests.cs`
- `AMDevIT.Restling.Tests/Models/SerializerSelectionModel.cs`
- `AMDevIT.Restling.Tests/Pipeline/TrackingResponseContent.cs`
- Corrected the existing multipart test HttpMethod alias to `AMDevIT.Restling.Core.HttpMethod`.

The two new suites contain 17 test methods / 49 data-expanded cases. They use in-memory message handlers, not httpbin. Coverage includes methods/URI/headers, payload and response shape, serializer precedence, null payloads, serialization failures, raw/form dispatch, pre-cancelled and in-flight cancellation, HTTP errors, decode errors, response disposal, HttpClient version defaults, and streaming early exit/failure.

## Verification and next step

- Fetched the remote; HEAD is three commits ahead of origin/main and zero behind. No pull/merge required.
- Compared the refactor with the pre-refactor RestlingClient implementation and corrected identified compatibility differences.
- Static checks: `git diff --check`, request-send call-site search, and new-code formatting review. Textual comparison found zero public/protected declaration differences; new source files contain no trailing-whitespace violations.
- After user authorization on 2026-09-02, restore and multi-target solution build passed with no warnings/errors. All 49 pipeline cases passed, along with codec, ownership, multipart, and XML security tests (74 total; 0 failed/skipped).
- Coverage measurement and a baseline-versus-refactor runtime comparison have not been performed. Keep httpbin integration tests separate. Consider the untyped-header payload anomaly as a separate follow-up.
