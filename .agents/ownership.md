# Explicit ownership

## Purpose

Restling now distinguishes borrowed resources from resources that it owns. This makes disposal predictable when callers reuse an `HttpClientContext`, `HttpClient`, or `HttpMessageHandler` across multiple client instances.

## Contracts

- `RestlingClientContextOwnership` controls whether `RestlingClient.Dispose()` disposes its context.
- Constructors that create a context internally own it.
- Constructors receiving an existing `HttpClientContext` borrow it unless ownership is explicitly set to `Owned`.
- `DisposeContext` remains a Boolean compatibility alias for `ContextOwnership`.
- `HttpClientContextOwnership` independently controls disposal of the `HttpClient` and message handler.
- The legacy three-argument `HttpClientContext` constructor retains its historical ownership of both resources.
- A context built by `HttpClientContextBuilder` always owns its generated `HttpClient`.
- Handlers created internally by the builder are owned; supplied handlers are borrowed by default and can be made owned explicitly.
- The legacy Boolean `AddHandler` overload maps to the new handler ownership enum.

## Verification state

Ownership regression tests have been added, but restore, build, and test execution remain deferred at the user's request.
