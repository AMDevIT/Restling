# Restling 

A lightweight, powerful, and easy-to-use REST client library for .NET.

## Overview
The goal of Restling is to provide a flexible yet easy-to-use REST client API. While it's true that there are plenty of REST client libraries out there, Restling wants to stand out by focusing on stability and offering support for uncommon features, such as cookie injection. It is also designed to integrate seamlessly into dependency injection-based projects as a transient service.

## Features  
- Lightweight and easy-to-use API.  
- Built-in support for cookie injection.  
- Designed for dependency injection (transient service).  
- Full support for .NET's `SocketsHttpHandler`.  
- Highly customizable via `HttpClientContextBuilder`.  
- Extensible content codecs with JSON, XML, text, and binary/raw defaults.

## Content codecs

Restling decodes responses by media type. Add custom codecs through `HttpClientContextBuilder.AddCodec`. Existing typed request payloads remain JSON unless `RestRequest<T>.UseContentCodec` is enabled explicitly. RFC 9457 Problem Details is available through `ProblemDetailsJsonCodec`, while CSV is supplied by the optional `Restling.Csv` package.

## Ownership and disposal

Clients created with the default constructor or a builder own their generated context. Clients receiving an existing `HttpClientContext` borrow it by default. Use `RestlingClientContextOwnership` to select the behavior explicitly and `HttpClientContextOwnership` to control disposal of the underlying `HttpClient` and handler. `DisposeContext` remains a compatibility alias.

## Multipart content

`MultipartRequest` sends form-data or any MIME multipart subtype with text, buffered binary, stream, arbitrary `HttpContent`, and codec-backed object parts. Buffered multipart responses deserialize to `MultipartDocument`, retaining ordered parts, duplicate names, headers, raw bytes, nested multipart content, related roots, and byte ranges. `multipart/x-mixed-replace` is available through a dedicated asynchronous streaming API.

## Why the name "Restling"?

While chatting with a friend, the name came to mind as a pun combining "REST" and "Changeling," the Fae beings from Northern folk tales. Restling is a library that has taken on the form of a REST client—though its journey started as something entirely different.

## Supported versions of DotNet

Restling supports both the latest .NET version and the latest LTS version, ensuring compatibility and stability for a wide range of projects.

*LTS Version:* .NET 10, 8
*STS Version:* .NET 9

## Installation

You can use Visual Studio solution package management or use the following commands:

### DotNet CLI

From a valid terminal:

```
dotnet add package Restling
```

### NuGet Package Manager

Using Visual Studio powershell package manager:

```
Install-Package Restling
```

## Basic usage:

The quickest way to use Restling is to allocate the client without parameters:

### Example 1: Basic Usage

```csharp

string uri;
RestlingClient restlingClient = new();
RestRequestResult restResponse;

// Call the uri resource without deserialization of the content
restResponse = await restlingClient.GetAsync(uri, cancellationToken)

```

Restling has a lot of customization options. You can use the HttpClientContextBuilder object to customize behavior of the client and http message handlers.
In the following example, we will instantiate a HttpClientContext using the HttpClientContextBuilder instance, with the following characteristics:

* SocketsHttpHandler: the new .NET message handler.
* CookieContainer: a cookie container initialized using a cookie storage from the application database.
* ILogger<RestClientController>: a logger for the current executing object.
* A custom user agent.
* A default header app-version with a valid version string set.

This code will allow the Restling client to send and receive cookies when a method is executed, adding the app-version header and setting a new user-agent.

`CookieStorageProvider` is primarily an in-memory provider. Its optional JSON file support is deliberately basic, runs only when the application explicitly calls `LoadAsync()` or `SaveAsync()`, and must not be treated as secure storage. Use the optional `Restling.Storage.Json` package for versioned JSON persistence, authenticated AES-256-GCM encryption, coalesced three-second auto-save, atomic replacement, and an orderly-shutdown flush. `Restling.Storage.Relational` provides the same auto-save lifecycle with SQLite, using either ordinary relational columns or application-encrypted rows selected explicitly for each database.

### Explicit proxy

```csharp
HttpClientContextBuilder builder = new();
builder.AddProxy("http://proxy.example.com:8080", allowAutoRedirect: false);
using RestlingClient client = new(builder);
```

`AddProxy(string proxyUri, bool allowAutoRedirect)` enables the proxy on a directly supplied `SocketsHttpHandler` or `HttpClientHandler`, or on the builder-created native handler. Cookie settings and ownership are preserved. `allowAutoRedirect` controls HTTP response redirects, not proxy bypass.

The address must be an absolute HTTP, HTTPS, SOCKS4, SOCKS4a, or SOCKS5 URI containing a host and optional port; embedded credentials, non-root paths, queries, and fragments are rejected. Native/platform transport support still applies. Configure proxy credentials through `ConfigureHandler` after `AddProxy` when needed.

Configure before the first request. `AddProxy` works before or after `AddHandler` and `ConfigureHandler`; later callback changes are retained by `Build`. A replacement native handler receives the last `AddProxy` selection. Custom/delegating handlers require explicit transport configuration and are rejected by this method. Existing custom builder implementations remain compatible through a default interface implementation that throws `NotSupportedException`.

`AddProxy` selects the context-wide default. Individual requests can override it without mutating the active context handler. Existing defaults remain unchanged when neither setting is used.

#### Per-request proxy override

All `RestRequest` types can override the context route:

```csharp
RestRequest request = new("https://api.example.com/status", HttpMethod.Get)
{
    ProxyOptions = RequestProxyOptions.Custom("http://another-proxy.example.com:8080",
                                              allowAutoRedirect: true)
};

RestRequestResult result = await client.ExecuteRequestAsync(request);
```

GET, POST, PUT, and DELETE convenience methods also expose proxy options, including typed and header variants. `CancellationToken` is always the final argument:

```csharp
RestRequestResult<ResourceModel> result = await client.GetAsync<ResourceModel>(uri,
                                                                                forcePayloadJsonSerializerLibrary: null,
                                                                                proxyOptions: RequestProxyOptions.Direct(),
                                                                                cancellationToken: cancellationToken);
```

The serializer parameter remains explicit in these new signatures, preventing ambiguity with existing positional `null` calls.

Use `RequestProxyOptions.Default` to retain the context transport, `Direct()` to disable explicit and system proxies, or `Custom(proxyUri)` for a dedicated proxy. Proxy and redirect combinations reuse isolated connection pools. Alternative transports share the context cookie jar, copy the default client's settings, and are disposed with the context. This applies to ordinary, raw, form-urlencoded, multipart, and mixed-replace streaming requests.

The default builder creates suitable alternative native handlers automatically. A supplied handler or one customized through `ConfigureHandler` cannot be cloned safely, so register `AddRequestHandlerFactory(cookieContainer => ...)` when overrides are required. Factory-created handlers are owned by the context; Restling applies routing, redirect, and shared-cookie settings. A factory may seed `Proxy.Credentials`, which Restling retains when selecting the request proxy. Default routing remains available without a factory, while an attempted override fails explicitly.

### Example 2: Advanced customization

```csharp

HttpClientContextBuilder contextBuilder = new();
SocketsHttpHandler httpHandler = new();
CookieContainer cookieContainer = this.RetrieveCookieContainerFromDatabase();
RestlingClient restlingClient;
ILogger<RestClientController> logger = services.GetRequiredService<ILogger<RestClientController>>();
RestRequestResult<ResourceModel> restResponse;

httpHandler.CookieContainer = cookieContainer;

contextBuilder.AddHandler(httpHandler, diposeHandler: true);
contextBuilder.AddDefaultHeader("app-version", "1.0.0");
contextBuilder.AddUserAgent("AppThatUsesREST/1.0 (Windows NT 10.0; Win64; x64)");

restlingClient = new(contextBuilder, logger);

restResponse = await restlingClient.GetAsync<ResourceModel>(uri, cancellationToken);

```
