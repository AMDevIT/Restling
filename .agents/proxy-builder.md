# Explicit proxy configuration

## Objective and status

Implemented the approved `HttpClientContextBuilder.AddProxy(string proxyUri, bool allowAutoRedirect)` API, interface member, documentation, and local regression tests. Per-request proxy overrides were explicitly deferred. All 161 selected local tests pass, including 43 new proxy cases.

## Decisions

- Configure directly supplied SocketsHttpHandler/HttpClientHandler instances without replacing their cookie container or changing ownership. Builder-created handlers remain owned.
- Require an absolute HTTP, HTTPS, SOCKS4, SOCKS4a, or SOCKS5 proxy URI. Reject embedded credentials, non-root paths, queries, and fragments; credentials can be supplied through ConfigureHandler on the native Proxy object.
- Enable UseProxy and apply the required allowAutoRedirect argument to HTTP response redirects. The argument does not control proxy bypass.
- Retain pending settings until native handler creation; apply them to a replacement AddHandler as well. Do not allocate an unused handler just to register the proxy.
- Later ConfigureHandler callbacks may override settings. Build does not reapply settings to an existing/active handler; borrowed transports can be reused across contexts without resetting proxy or cookies.
- Reject unsupported custom/delegating handlers explicitly. Do not inspect opaque handler chains or silently bypass an explicit proxy request.
- Proxy changes after transport startup retain the native InvalidOperationException behavior. Failed URI validation or unsupported-handler selection leaves the builder selection unchanged.
- Preserve custom builder compatibility with a default interface implementation that throws NotSupportedException.
- Without AddProxy, historical native proxy and redirect defaults are unchanged. Separate contexts/handlers remain the supported way to select a different proxy or direct connections; no per-request override was implemented.

## Affected files

- Core Network/Builders/HttpClientContextBuilder.cs and IHttpClientContextBuilder.cs.
- Tests/ProxyBuilderTests.cs; reuses the existing loopback HTTP script server without changing it.
- Root README.md and Assets/Documentation/README.md.
- This note and .agents/context.md.

## Verification (2026-09-02)

- Fetch succeeded; Task-NewCodecs and its upstream were aligned, with no pull or merge required. Worktree was initially clean.
- Authorized restore passed. Initial test compilation used an assertion method unavailable to the resolved test framework; corrected to the existing ThrowsException API.
- Final solution build: Core/CSV net8.0, net9.0, net10.0 and tests net10.0; 0 warnings/errors.
- Initial targeted proxy run: 38/38 passed. Five additional credential/default-policy cases were then added.
- Final selected regression suite: 161/161 passed, 0 failed/skipped (118 existing + 43 proxy cases).
- Loopback HTTP proxy checks exercise both native handlers, enabled/disabled redirects, absolute request targets, response cookies, consecutive requests, active-handler mutation rejection, and context recreation with a borrowed handler. No external destination or proxy is contacted.
- Scheme validation, URI rejection, call ordering, handler replacement, explicit cookie containers, ownership flags, credential configuration, and historical defaults are covered by configuration tests.
- git diff --check passed. Checked new Markdown C# calls for repository formatting.
- Reports: TestResults/proxy/proxy-targeted.trx (initial 38 cases) and TestResults/proxy/proxy-regression.trx (final 161 cases).

## Remaining scope

Per-request proxy routing was implemented in the subsequent `request-proxy.md` step. HTTPS CONNECT/TLS proxy and SOCKS handshakes, proxy authentication exchanges, httpbin integration tests, other runtime versions, and mobile/platform-specific handlers were not exercised. Configuration tests do not establish those transport integrations work on every platform.
