# Cookie builder correction

## Objective and status

The user explicitly approved correcting the cookie builder after the loopback tests exposed eight failures. The correction is implemented and verified: all 118 selected local regression cases pass, including all 36 cookie cases.

## Decisions

- `ResolveCookieContainer` is the single binding point for directly supplied SocketsHttpHandler and HttpClientHandler instances and handlers created through ConfigureHandler.
- An explicit AddCookieContainer selection takes precedence, regardless of whether it precedes or follows AddHandler. It is also available to a newly created ConfigureHandler callback.
- Without an explicit container, the native handler's existing jar is used by the context and by AddCookie/AddCookies. Existing cookie state is not replaced.
- Replacing a handler without an explicit container adopts the replacement handler's jar rather than copying unrelated cookie state.
- Selecting an explicit container enables native cookie handling, matching the previous AddCookieContainer behavior. Build does not subsequently override an intentional UseCookies=false setting made in ConfigureHandler. Without an explicit container, native UseCookies is preserved.
- Container assignments and UseCookies setters are skipped when no change is necessary, allowing repeated Build calls with a previously started borrowed handler.
- A separate fallback container preserves existing custom-handler behavior without turning an implicit fallback into an explicit override for later native handlers.
- Cookie clearing selects a new jar without destroying the old externally owned jar.
- Redirect and resource ownership policies are unchanged. Native handlers continue to enforce response-cookie scope, Secure, and deletion rules; no manual Cookie header forwarding is introduced.
- Opaque custom handlers and delegating-handler chains are not introspected; they remain responsible for their own cookie processing. This change targets the confirmed direct native-handler/container binding defect, not all cookie metadata/API semantics.

## Affected files

- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/Network/Builders/HttpClientContextBuilder.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Tests/CookieBuilderTests.cs` (18 new cases)
- Existing `CookiePersistenceTests` (18 cases, previously 8 failing) run unchanged.
- Progressive context and prior follow-up status notes.

## Verification

- Fetch succeeded: HEAD is 4 commits ahead of origin/main, 0 behind; no pull/merge required.
- Restore succeeded with authorized access to NuGet configuration/cache.
- Targeted cookie run: 36 passed, 0 failed/skipped.
- Full solution build: Core/CSV net8.0, net9.0, net10.0 and tests net10.0; 0 warnings and 0 errors.
- Full selected regression run: 118 passed, 0 failed, 0 skipped on net10.0. This includes 57 pipeline, 6 codec, 7 ownership, 6 multipart, 6 XML security, 18 cookie persistence, and 18 cookie builder cases.
- The original eight cookie reproductions are now green, including supplied/configured handlers, AddCookieContainer/AddHandler ordering, consecutive calls, and automatic redirects.
- Loopback tests also cover manual redirects, path/domain/Secure filtering of response cookies, deletion, client recreation, and rebuilding with an active handler.
- `git diff --check` passed.
- Reports: `TestResults/cookie-builder/cookie-builder-targeted.trx` and `TestResults/cookie-builder/cookie-builder-regression.trx`.

## Remaining scope

No known failures remain in the selected local suites. httpbin integration tests, runtime execution on other frameworks/mobile platforms, coverage measurement, and general serializer-precedence normalization remain outside this change.
