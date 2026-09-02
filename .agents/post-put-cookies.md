# POST/PUT header payload fix and cookie verification

## Objective and status

The user approved fixing the untyped POST/PUT overloads with headers, building, and running local tests. The user also requested checking cookies across redirects, responses, and consecutive calls.

- POST/PUT fix: implemented and verified.
- Cookie investigation initially confirmed a separate builder defect with eight reproducible failing tests. The user subsequently approved the correction; it is now complete, and all tests pass. See `cookie-builder.md` for implementation details and the latest 118-case verification.

## Implementation decisions

- Both affected overloads now use a shared private `ExecuteHeaderPayloadRequestAsync<T>` helper.
- The helper builds payload content and request headers with the existing builder, then runs the untyped centralized HTTP pipeline.
- Public signatures are unchanged. Returned objects are now `RestRequestResult`, not an incidental typed result attempting to decode the response as the request model.
- Per-request authentication, headers, explicit request serializer, client-default request metadata behavior, cancellation, and send-error handling are covered by tests.
- Null payloads retain the bodyless behavior of explicitly constructed header requests. Other overloads and serializer precedence were not normalized in this fix.

## Cookie findings

Tests use an ephemeral IPv4 loopback TCP server and real SocketsHttpHandler/HttpClientHandler instances. They do not use httpbin or bypass native cookie processing with a fake handler.

- Default builder: seeded cookies and Set-Cookie updates persist across consecutive calls and recreated RestlingClient instances sharing one context.
- Manual redirects: response cookies are stored before following Location for 301, 302, 303, 307, and 308. Default automatic redirects remain disabled.
- Automatic redirects with an explicitly configured handler work when AddCookieContainer is called after AddHandler. Cookies from intermediate/final responses are reused on subsequent calls.
- Confirmed defect: when AddCookieContainer precedes AddHandler, the supplied container is not attached to the handler. Seeded cookies are missing on the wire.
- Confirmed defect: AddHandler and ConfigureHandler paths can leave the context cookie container separate from the handler container. Cookies added through the builder are then not sent. Six consecutive-call cases fail across SocketsHttpHandler, HttpClientHandler, and ConfigureHandler; two automatic-redirect cases fail due to builder call order.
- Native response-cookie path/domain/Secure/deletion rules pass in the default path. No manual Cookie forwarding was added.
- Proposed follow-up: synchronize the effective cookie container during handler setup/build, retain pre-existing handler cookies when no explicit container is supplied, and preserve handler redirect/ownership choices. Custom delegating handler chains and broader cookie configuration options have not been exhaustively assessed.

## Affected files

- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/RestlingClient.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Tests/HttpPipelineCompatibilityTests.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Tests/CookiePersistenceTests.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Tests/Cookies/LoopbackCookieServer.cs`
- Progressive context and pipeline notes.

## Verification

- Fetch succeeded; HEAD is 4 commits ahead of origin/main, 0 behind. No pull required.
- Before the production fix, all 4 new body/serializer cases failed with a null outgoing body; after the fix, all pass.
- Added 2 null-payload and 4 error/cancellation cases. Corrected an initial test assumption: HttpClient can invoke a custom handler with a cancelled token; the test handler now observes that token, as real transports do.
- Restore succeeded. Full solution build passed for Core/CSV net8.0, net9.0, net10.0 and tests net10.0, with 0 warnings/errors.
- Latest combined run: 100 cases, 92 passed, 8 failed, 0 skipped. All 82 non-cookie cases passed, including 57 pipeline cases. Cookie suite: 18 cases, 10 passed, 8 failed as described above.
- Reports: `TestResults/post-put-cookies/post-put-cookie-initial.trx` and `TestResults/post-put-cookies/post-put-cookie-regression.trx`.
- httpbin integration tests were excluded. Runtime tests targeted net10.0 only; cookie checks used loopback networking on Windows.

## Next step

Completed after explicit confirmation: fixed cookie binding and reran the unchanged eight reproductions plus additional preservation tests. All 118 selected cases now pass; the earlier failing results above are retained as the diagnostic history.
