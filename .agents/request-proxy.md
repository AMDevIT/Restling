# Per-request proxy routing

## Objective and status

Implemented request-level routing overrides with `Default`, `Direct`, and `Custom` proxy modes. The change is documented and verified: 177 selected local regression tests pass, including 16 new request-proxy cases and the 43 existing context-proxy cases.

## Decisions

- `RestRequest.ProxyOptions` applies to every request subtype, including raw, form-urlencoded, multipart, and multipart/x-mixed-replace streaming requests. Its default preserves the context transport unchanged.
- `RequestProxyOptions` is an immutable value object. Equivalent mode/URI/redirect selections share one cached alternative HttpClient and connection pool.
- `Direct` sets UseProxy=false and therefore bypasses explicit and system proxies. `Custom` uses a validated HTTP, HTTPS, SOCKS4, SOCKS4a, or SOCKS5 URI. Both explicitly select AllowAutoRedirect.
- The centralized pipeline resolves the transport immediately before sending. Buffered transport-selection failures remain result-based; streaming failures retain their throwing contract.
- Alternative clients copy the context client's BaseAddress, request version/policy, response buffer limit, timeout, and default headers. Their native handlers share the exact context CookieContainer.
- The context owns alternative clients and handlers independently from the ownership of the default transport and disposes them once.
- A default builder supplies a native alternative-handler factory automatically. AddHandler or ConfigureHandler disables implicit recreation because arbitrary external TLS, certificate, pooling, platform, and delegating-handler settings cannot be cloned safely.
- `AddRequestHandlerFactory` explicitly enables overrides for externally supplied/configured handlers. It receives the shared cookie container, must return a fresh SocketsHttpHandler or HttpClientHandler, and transfers credentials from a factory-seeded Proxy to the selected custom proxy.
- Missing/invalid factories fail explicitly rather than silently using the context route. Existing default calls and existing builder/context constructors remain source and binary compatible.
- Direct convenience methods were not expanded with many overloads; callers express routing consistently through RestRequest and ExecuteRequestAsync, including specialized request APIs.

## Affected files

- Added Network/RequestProxyMode.cs, RequestProxyOptions.cs, ProxyUriParser.cs, and RequestTransportPool.cs.
- Updated RestRequest, HttpExecutionPipeline, RestlingClient, HttpClientContext, HttpClientContextBuilder, and IHttpClientContextBuilder.
- Added RequestProxyOverrideTests and Proxy/TrackingHttpClientHandler; extended the existing loopback response helper.
- Updated both repository/package READMEs, this note, proxy-builder.md, and context.md.

## Verification (2026-09-03)

- After the user pulled two remote commits, fetch confirmed Task-NewCodecs was aligned with its upstream and the starting worktree was clean.
- Authorized restore succeeded.
- Final solution build passed for Core/CSV net8.0, net9.0, net10.0 and tests net10.0 with 0 warnings/errors. An earlier test-triggered incremental build emitted CS8892 from generated MSTest entry-point files after the upstream dependency/project update; it was absent from the final build and did not originate in modified Restling source.
- Targeted final run: 59/59 request/context proxy cases passed.
- Selected regression run: 177/177 passed, 0 failed/skipped (161 prior cases + 16 request-proxy cases).
- Loopback tests cover default/custom/direct route isolation, absolute versus origin request targets, shared response cookies, copied headers, proxy/redirect cache keys, native factory invocation, missing/invalid factories, alternative ownership/disposal, buffered specialized requests, and mixed-replace streaming.
- No external proxy or destination was contacted. Reports are in TestResults/request-proxy/.
- git diff --check passed.

## Remaining scope

Runtime tests still target net10.0 on Windows. HTTPS CONNECT/TLS proxy and SOCKS handshakes, real proxy authentication exchanges, mobile/platform handlers, convenience-method overloads, bounded/expiring transport-cache policies, and integration tests against external services remain outside this step.
