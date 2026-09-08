[![NuGet](https://img.shields.io/nuget/v/Restling)](https://www.nuget.org/packages/Restling)
[![Downloads](https://img.shields.io/nuget/dt/Restling)](https://www.nuget.org/packages/Restling)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com)

# Restling

![Restling Icon](Assets/Icons/RestlingIcon.png)

Restling is a lightweight and configurable REST client library for .NET. It provides typed and untyped HTTP operations, request-level and client-level headers, JSON and XML payload handling, raw and form URL-encoded requests, cookie injection and persistence, and direct access to response metadata.

Restling targets .NET 8, .NET 9, and .NET 10 and can be used by .NET for Android, .NET for iOS, and .NET MAUI applications.

## Features

- Async GET, POST, PUT, DELETE, raw, and form URL-encoded requests.
- Typed response deserialization and access to the original response content.
- Automatic JSON serializer selection, with explicit System.Text.Json or Newtonsoft.Json overrides.
- XML deserialization with external entity processing disabled by default.
- Default and per-request headers, including authentication headers.
- Custom `HttpMessageHandler`, timeout, user agent, and cookie configuration.
- Cookie injection and optional file-backed cookie storage.
- Response status, timing, headers, redirect location, charset, raw bytes, and exceptions.
- URI validation enabled by default, with custom validation support.

## Installation

Install the [`Restling`](https://www.nuget.org/packages/Restling) package.

### .NET CLI

```shell
dotnet add package Restling
```

### NuGet Package Manager

```powershell
Install-Package Restling
```

## Quick start

Add the core namespace and create a client:

```csharp
using AMDevIT.Restling.Core;

RestlingClient client = new();
```

### GET a typed resource

```csharp
using AMDevIT.Restling.Core;

RestlingClient client = new();
RestRequestResult<TodoItem> result;

result = await client.GetAsync<TodoItem>("https://api.example.com/todos/1",
                                         cancellationToken: cancellationToken);

if (result.IsSuccessful && result.Data is not null)
{
    Console.WriteLine(result.Data.Title);
}
else
{
    Console.WriteLine($"Request failed: {result.StatusCode} - {result.Exception}");
}
```

`RestRequestResult<T>` exposes the deserialized value through `Data`. It also retains the status code, elapsed time, response headers, string content, raw bytes, content type, charset, and any exception raised while executing or decoding the request.

### POST JSON and deserialize the response

The first generic type is the response model and the second is the request model:

```csharp
CreateTodoRequest payload = new("Read the Restling wiki", Completed: false);
RestRequestResult<TodoItem> result;

result = await client.PostAsync<TodoItem, CreateTodoRequest>("https://api.example.com/todos",
                                                             payload,
                                                             cancellationToken: cancellationToken);
```

### Send request-specific headers

```csharp
using AMDevIT.Restling.Core.Network;

RequestHeaders headers = new(new AuthenticationHeader("Bearer", accessToken));
headers.Headers.Add("X-Correlation-ID", correlationId);

RestRequestResult<TodoItem> result = await client.GetAsync<TodoItem>("https://api.example.com/todos/1",
                                                                     headers,
                                                                     cancellationToken: cancellationToken);
```

## Configure a client

Use `HttpClientContextBuilder` when settings must apply to every request made by a client:

```csharp
using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network.Builders;

HttpClientContextBuilder builder = new();

builder.AddUserAgent("MyApp/1.0")
       .AddDefaultHeader("X-App-Version", "1.0.0")
       .AddAuthenticationHeader("Bearer", accessToken)
       .SetTimeout(TimeSpan.FromSeconds(30));

RestlingClient client = new(builder);
```

You can also supply or configure an `HttpMessageHandler`:

```csharp
HttpClientContextBuilder builder = new();

builder.ConfigureHandler(handler =>
{
    if (handler is SocketsHttpHandler socketsHandler)
    {
        socketsHandler.AllowAutoRedirect = true;
        socketsHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);
    }
});

RestlingClient client = new(builder);
```

### Explicit proxy

```csharp
HttpClientContextBuilder builder = new();
builder.AddProxy("http://proxy.example.com:8080", allowAutoRedirect: false);
using RestlingClient client = new(builder);
```

`AddProxy(string proxyUri, bool allowAutoRedirect)` enables an explicit proxy on a directly supplied `SocketsHttpHandler` or `HttpClientHandler`, or on the native handler created by the builder. It preserves handler ownership and cookie settings. The boolean controls automatic HTTP response redirects, not proxy bypass.

Use an absolute `http`, `https`, `socks4`, `socks4a`, or `socks5` URI with a host and optional port; embedded credentials, non-root paths, queries, and fragments are rejected. These schemes are supported by the [.NET proxy transport](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.defaultproxy?view=net-10.0); availability on platform-specific handlers depends on the runtime. For authentication, configure the native handler's `Proxy.Credentials` through `ConfigureHandler` after `AddProxy`.

Call `AddProxy` before sending requests. It works before or after `AddHandler` and `ConfigureHandler`; a later `ConfigureHandler` callback can override its settings, and `Build` does not reset them. Replacing the native handler applies the last `AddProxy` selection. Opaque custom/delegating handlers throw `NotSupportedException` instead of silently ignoring the proxy. Without `AddProxy`, existing transport defaults are unchanged.

`AddProxy` selects the context-wide default. Individual `RestRequest` instances can override it without mutating the shared active handler, as described below.

### Per-request proxy override

Every `RestRequest`, including raw, form-urlencoded, multipart, and mixed-replace streaming requests, can select a route independently:

```csharp
RestRequest directRequest = new("https://api.example.com/status", HttpMethod.Get)
{
    ProxyOptions = RequestProxyOptions.Direct(allowAutoRedirect: false)
};

RestRequest proxiedRequest = new("https://api.example.com/status", HttpMethod.Get)
{
    ProxyOptions = RequestProxyOptions.Custom("http://another-proxy.example.com:8080",
                                              allowAutoRedirect: true)
};

RestRequestResult directResult = await client.ExecuteRequestAsync(directRequest);
RestRequestResult proxiedResult = await client.ExecuteRequestAsync(proxiedRequest);
```

The direct GET, POST, PUT, and DELETE methods provide the same selection, including typed and per-request-header variants. `CancellationToken` remains the final parameter in every overload:

```csharp
RequestProxyOptions proxyOptions = RequestProxyOptions.Custom("socks5://127.0.0.1:1080");

RestRequestResult<TodoItem> getResult = await client.GetAsync<TodoItem>(uri,
                                                                       forcePayloadJsonSerializerLibrary: null,
                                                                       proxyOptions: proxyOptions,
                                                                       cancellationToken: cancellationToken);

RestRequestResult postResult = await client.PostAsync(uri,
                                                      payload,
                                                      forcePayloadJsonSerializerLibrary: null,
                                                      proxyOptions: proxyOptions,
                                                      cancellationToken: cancellationToken);
```

The serializer argument is explicit in these overloads so existing calls that pass `null` positionally remain unambiguous and source-compatible.

`RequestProxyOptions.Default` (the initial value) uses the context transport unchanged. `Direct` disables both explicit and system proxies. `Custom` uses its dedicated proxy. Equivalent selections reuse one connection pool, while different proxy or redirect settings remain isolated. Alternative transports share the context cookie container and copy its `HttpClient` defaults; the context owns and disposes them.

The default builder supplies alternative native handlers automatically. When an external handler or `ConfigureHandler` is used, provide an explicit factory so Restling does not guess how to clone TLS, certificate, pooling, or platform-specific settings:

```csharp
builder.AddHandler(sharedHandler)
       .AddRequestHandlerFactory(cookieContainer => new SocketsHttpHandler
       {
           CookieContainer = cookieContainer,
           PooledConnectionLifetime = TimeSpan.FromMinutes(5)
       });
```

The factory creates fresh handlers owned by the context. Restling applies the selected proxy, redirect policy, and shared cookie container after creation. To provide proxy credentials, initialize `Proxy.Credentials` in the factory; the credentials are transferred to the request-selected proxy address. Without a factory, default requests still work, while a `Direct` or `Custom` override returns a `NotSupportedException` failure. Buffered requests retain result-based transport failures; streaming transport failures continue to throw.

## Ownership and disposal

A client created with its default constructor or with `HttpClientContextBuilder` owns the generated context and disposes it:

```csharp
using RestlingClient client = new();
```

A client constructed with an existing context borrows it by default. Disposing the client leaves the context, its `HttpClient`, and its handler available for reuse:

```csharp
HttpClientContext sharedContext = builder.Build();

using (RestlingClient client = new(sharedContext))
{
    await client.GetAsync("https://api.example.com/status");
}

// sharedContext is still owned by the caller.
```

Ownership can be selected explicitly:

```csharp
RestlingClient client = new(sharedContext, RestlingClientContextOwnership.Owned);
```

`DisposeContext` remains available as a compatibility alias. At the context level, `HttpClientContextOwnership` independently controls disposal of `HttpClient` and `HttpMessageHandler`. A builder-created context always owns its `HttpClient`; handler ownership can be selected with `HttpMessageHandlerOwnership.Borrowed` or `Owned`. The old boolean `AddHandler` overload remains supported.

## Multipart content

`MultipartRequest` creates fresh content for every execution. Buffered parts are reusable; stream and arbitrary content factories return instances owned and disposed by that execution:

```csharp
MultipartRequest request = new("https://api.example.com/documents", HttpMethod.Post);
request.AddText("description", "Quarterly report")
       .AddObject("metadata", metadata, HttpMediaType.ApplicationJson)
       .AddStream("document",
                  () => File.OpenRead(documentPath),
                  "report.pdf",
                  HttpMediaType.ApplicationPdf);

RestRequestResult<UploadResult> result = await client.ExecuteMultipartRequestAsync<UploadResult>(request);
```

Buffered `multipart/*` responses can be decoded as `MultipartDocument`. Parts retain their order, duplicate names, headers and original bytes. Each part can be decoded through the same codec registry:

```csharp
RestRequestResult<MultipartDocument> result = await client.GetAsync<MultipartDocument>(uri);

foreach (MultipartPart part in result.Data?.Parts ?? [])
{
    byte[] original = part.RawContent;
    Metadata? metadata = part.ContentType?.MediaType == HttpMediaType.ApplicationJson
        ? part.Deserialize<Metadata>()
        : null;
}
```

Nested multipart entities are exposed through `NestedContent`; `RootPart` resolves the `start` parameter of `multipart/related`, and `ContentRange` exposes `multipart/byteranges` metadata. File names are untrusted metadata and are never interpreted as local paths.

`multipart/signed` and `multipart/encrypted` are parsed structurally, but Restling does not verify signatures or decrypt their parts.

Parsing defaults can be replaced by registering `new MultipartContentCodec(new MultipartOptions { ... })` before the standard codecs.

`multipart/x-mixed-replace` has a dedicated streaming API because the response can be unbounded:

```csharp
RestRequest streamRequest = new(uri, HttpMethod.Get);

await foreach (MultipartPart part in client.StreamMultipartMixedReplaceAsync(streamRequest, cancellationToken: cancellationToken))
{
    ProcessFrame(part.RawContent, part.ContentType);
}
```

## More request types

### Raw content

```csharp
using AMDevIT.Restling.Core.Network;

RestRawRequest request = new("https://api.example.com/events",
                             AMDevIT.Restling.Core.HttpMethod.Post,
                             content: json,
                             contentType: HttpMediaType.ApplicationJson);

RestRequestResult<ApiResponse> result = await client.ExecuteRawRequestAsync<ApiResponse>(request,
                                                                                         cancellationToken: cancellationToken);
```

### Form URL-encoded content

```csharp
IDictionary<string, string> fields = new Dictionary<string, string>
{
    ["grant_type"] = "client_credentials",
    ["scope"] = "api.read"
};

FormUrlEncodedRequest request = new("https://identity.example.com/token",
                                    AMDevIT.Restling.Core.HttpMethod.Post,
                                    fields);

RestRequestResult<TokenResponse> result = await client.ExecuteFormUrlEncodedRequest<TokenResponse>(request,
                                                                                                   cancellationToken: cancellationToken);
```

## JSON serializer selection

Restling automatically selects between Newtonsoft.Json and System.Text.Json by inspecting the model. You can set a client-wide default or override it for an individual request:

```csharp
using AMDevIT.Restling.Core.Serialization;

client.SelectedDefaultSerializationLibrary = PayloadJsonSerializerLibrary.SystemTextJson;

RestRequestResult<TodoItem> result = await client.GetAsync<TodoItem>("https://api.example.com/todos/1",
                                                                     forcePayloadJsonSerializerLibrary: PayloadJsonSerializerLibrary.NewtonsoftJson,
                                                                     cancellationToken: cancellationToken);
```

## Extensible content codecs

Restling selects response decoders by media type. Every client context includes JSON (including `application/*+json`), XML (including `application/*+xml`), text, and binary/raw codecs by default, so existing requests require no configuration. Problem media types remain opt-in. Custom codecs have priority when added through `HttpClientContextBuilder`:

```csharp
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Network.Builders;

HttpClientContextBuilder builder = new();
builder.AddCodec(new MyContentCodec());
```

Request models remain JSON by default for backward compatibility. To serialize a model with another registered codec, set its media type and `UseContentCodec`:

```csharp
RestRequest<ImportModel> request = new("https://api.example.com/import",
                                       AMDevIT.Restling.Core.HttpMethod.Post,
                                       model)
{
    ContentMediaType = HttpMediaType.ApplicationXml,
    UseContentCodec = true
};
```

CSV is supplied by the optional `Restling.Csv` project and is registered with `builder.AddCodec(new CsvContentCodec())`. RFC 9457 Problem Details is enabled with `builder.AddCodec(new ProblemDetailsJsonCodec())`; structured errors appear in `RestRequestResult.Problem`, while malformed problem documents appear in `ProblemException` without losing the HTTP response.

## Cookies

Inject individual cookies or a complete `CookieContainer` through the builder:

```csharp
using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Network.Builders;

HttpCookieData sessionCookie = new("session-id",
                                   sessionId,
                                   domain: "api.example.com",
                                   path: "/",
                                   isSecure: true);

HttpClientContextBuilder builder = new();
builder.AddCookie(sessionCookie);

RestlingClient client = new(builder);
```

`CookieStorageProvider` is primarily an in-memory cookie jar. It can optionally load and save a simple JSON file when the application explicitly calls `LoadAsync()` or `SaveAsync()`. This basic format is not intended to be a secure store and Restling never saves it automatically.

Install `Restling.Storage.Json` when cookies require versioned, extensible persistence. `JsonCookieStorageProvider` can write plain JSON or an AES-256-GCM envelope whose random data-encryption key is protected by an application-supplied `IDataEncryptionKeyProtector`. Automatic persistence is enabled by default and coalesces cookie changes into one atomic flush every three seconds. `SaveAsync()` flushes immediately and disposing an owning client flushes pending changes during orderly shutdown.

```csharp
JsonCookieStorageOptions storageOptions = new()
{
    FilePath = cookieFilePath,
    DataEncryptionKeyProtector = keyProtector
};
JsonCookieStorageProvider storage = new(storageOptions);
HttpClientContextBuilder builder = new();
builder.AddCookieStorageProvider(storage);
using RestlingClient client = new(builder);
```

## Result handling

Restling returns a result object for HTTP failures and for most execution or decoding errors. Check `IsSuccessful`, then inspect `StatusCode` and `Exception`:

```csharp
RestRequestResult<TodoItem> result = await client.GetAsync<TodoItem>(uri,
                                                                     cancellationToken: cancellationToken);

if (!result.IsSuccessful)
{
    Console.WriteLine(result.StatusCode);
    Console.WriteLine(result.Exception?.Message);
    Console.WriteLine(result.Content);
    return;
}

TodoItem? todo = result.Data;
```

## Documentation

See the [Restling GitHub Wiki](https://github.com/AMDevIT/Restling/wiki) for installation guidance, complete quick starts, client configuration, request types, authentication, serialization, cookies, response handling, and security notes.

## Why the name "Restling"?

The name combines "REST" and "Changeling", the Fae beings from Northern folk tales. Restling is a library that took the form of a REST client, although its journey began as something entirely different.

## License

Restling is distributed under the [MIT License](LICENSE).
