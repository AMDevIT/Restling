# Status-based response data mapping

## Objective and status

Implement issue #37 with an opt-in fluent API that selects a response data type from the HTTP status code. The implementation, documentation, targeted tests, multi-target build, and relevant local regression verification are complete.

## Decisions

- Response models do not implement a marker interface. Any type supported by the selected content codec can be registered, including classes, records, strings, primitives, and byte arrays.
- Every `RestRequest` owns a `ResponseMappingCollection`. Existing direct overloads and requests without a matching mapping retain their historical behavior.
- Registrations support an exact `HttpStatusCode`, an inclusive range, standard 1xx-5xx classes, the combined 400-599 error range, and a fallback.
- Exact mappings take priority over non-exact patterns. Overlapping non-exact patterns use registration order, and fallback is evaluated last. Re-registering an exact status replaces its previous mapping.
- The collection uses immutable array snapshots under a lock so reads are safe while registrations replace the current snapshot.
- A matched typed response is decoded once into `RestRequestResult.MappedData`; `RestRequestResult<T>.Data` remains at its default value.
- `MappedDataType` identifies the selected registration, `TryGetMappedData<T>` provides typed access, and `MappedDataException` exposes decoding failures without replacing HTTP status, headers, raw content, or Problem Details.
- Each internal generic registration retains a strongly typed `IContentCodec.Deserialize<T>` delegate. No reflection, `IContentCodec` change, `IRestlingClient` change, or overload expansion is required.
- Streaming multipart responses remain outside the mapping contract because they do not produce a single buffered `RestRequestResult`.

## Affected files

- Added `Responses/ResponseStatusPattern.cs`, `ResponseMapping.cs`, and `ResponseMappingCollection.cs` in Core.
- Updated `RestRequest`, `RestRequestResult`, and `HttpResponseParser`.
- Added `StatusResponseMappingTests.cs` with 18 deterministic cases.
- Updated the repository and NuGet package READMEs.
- Added this note and updated `.agents/context.md`.

## Verification

- Fetch confirmed `Task-Issue37` was aligned with its upstream and based on the latest `origin/main`; no pull or merge was required during completion.
- `dotnet restore Sources\AMDevIT.Restling` passed.
- The complete solution build passed for Core, CSV, JSON storage, and relational storage on net8.0, net9.0, and net10.0 where configured, plus tests on net10.0, with 0 warnings and 0 errors.
- The first targeted run exposed five test-fixture casing errors against the existing case-sensitive System.Text.Json behavior; production code was unchanged and the fixtures were corrected.
- The final status-response suite passed 18/18 on net10.0.
- The relevant selected local regression passed 202/202 on net10.0, covering response mapping, codecs, pipeline, XML security, ownership, multipart, cookies, proxy routing, ResponseEnded recovery, and JSON cookie storage.
- A broader 208-case run initially passed 204 tests and exposed four SQLite test-helper file-lock failures. Disabling connection pooling in those helpers resolved the independent cleanup problem; the isolated SQLite suite now passes 6/6.
- The initial `dotnet test` invocation exited without executing or producing a report under the resolved test tooling, so verification used `dotnet vstest` against the built net10.0 assembly.
- Reports are under `TestResults/status-response/`.

## Open issues and recommended next step

- The status-response implementation has no known failures in the relevant local suites.
- External httpbin integration tests and runtime execution on net8.0, net9.0, iOS, Android, and MAUI remain outside this run.
- Review the public naming and fluent API, then commit the progressive-context changes if accepted.
