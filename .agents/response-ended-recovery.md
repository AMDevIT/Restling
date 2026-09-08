# Request transport recovery after truncated responses

## Objective and status

Recover request-specific transports after .NET reports `HttpRequestError.ResponseEnded`. The implementation and a deterministic loopback regression are complete. Build and test execution were not performed at the user's request.

## Decisions

- The HTTP pipeline inspects the complete exception chain so it recognizes the `HttpIOException` wrapped by `HttpRequestException`.
- Only `ResponseEnded` triggers eviction. Authentication failures, cancellations, DNS failures, and other transport errors retain the existing cache behavior.
- The default context client is never invalidated or disposed by this recovery path.
- Direct and custom request-specific transports are evicted only if the failed `HttpClient` is still the cached instance for the immutable proxy selection. This prevents an older concurrent failure from evicting a replacement transport.
- Eviction disposes the failed alternative client and its owned native handler. The caller's existing retry policy then resolves a fresh handler, connection pool, and SOCKS tunnel.
- Restling does not add an internal retry, preserving request replay and retry-policy ownership at the caller boundary.
- The recovery path does not modify authentication or other request headers.

## Affected files

- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/Network/RequestTransportPool.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/Network/Builders/HttpClientContext.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/Network/Pipeline/HttpExecutionPipeline.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Core/RestlingClient.cs`
- `Sources/AMDevIT.Restling/AMDevIT.Restling.Tests/RequestProxyOverrideTests.cs`

## Verification and next step

- The preliminary fetch completed; `Task-NewCodecs` was clean and aligned with its upstream before editing.
- The resulting diff was inspected after applying the targeted patch.
- No restore, build, or tests were run at the user's request.
- Recommended next step: after authorization, run the targeted `ResponseEndedInvalidatesAlternativeTransport` regression, the proxy suite, and the multi-target solution build.
