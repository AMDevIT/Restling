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

Restling also provides `CookieStorageProvider` for loading and saving cookies to a JSON file. Storage encryption is available by supplying an `ICookieStorageProviderEncrypter` implementation.

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
