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
dotnet add package AMDevIT.Restling.Core
```

### NuGet Package Manager

Using Visual Studio powershell package manager:

```
Install-Package AMDevIT.Restling.Core
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