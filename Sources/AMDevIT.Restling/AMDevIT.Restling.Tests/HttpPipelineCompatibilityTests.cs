using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Tests.Models;
using AMDevIT.Restling.Tests.Multipart;
using AMDevIT.Restling.Tests.Pipeline;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class HttpPipelineCompatibilityTests
    {
        #region Methods

        /// <summary>Verifies the historical verb, URI and untyped result contract of direct bodyless methods.</summary>
        [TestMethod]
        [DataRow("GET")]
        [DataRow("DELETE")]
        public async Task DirectBodylessMethodsPreserveRequestAndResult(string method)
        {
            const string uri = "https://example.test/resource?value=7";
            string? capturedMethod = null;
            Uri? capturedUri = null;
            using RecordingMessageHandler handler = new((request, _) =>
            {
                capturedMethod = request.Method.Method;
                capturedUri = request.RequestUri;
                return Task.FromResult(CreateTextResponse(HttpStatusCode.Accepted, "historic"));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context)
            {
                SelectedDefaultSerializationLibrary = PayloadJsonSerializerLibrary.NewtonsoftJson
            };
            RestRequestResult result = method == "GET"
                ? await client.GetAsync(uri)
                : await client.DeleteAsync(uri);

            Assert.AreEqual(method, capturedMethod);
            Assert.AreEqual(uri, capturedUri?.AbsoluteUri);
            Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            Assert.AreEqual("historic", result.Content);
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsNull(result.Exception);
            Assert.IsNull(result.Request.ForcePayloadJsonSerializerLibrary);
        }

        /// <summary>Verifies the historical JSON payload and typed result contract of direct POST and PUT methods.</summary>
        [TestMethod]
        [DataRow("POST")]
        [DataRow("PUT")]
        public async Task DirectPayloadMethodsPreserveRequestAndTypedResult(string method)
        {
            const string uri = "https://example.test/resource";
            string? capturedBody = null;
            string? capturedMethod = null;
            string? capturedMediaType = null;
            using RecordingMessageHandler handler = new(async (request, cancellationToken) =>
            {
                capturedMethod = request.Method.Method;
                capturedMediaType = request.Content?.Headers.ContentType?.MediaType;
                capturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return CreateJsonResponse(HttpStatusCode.OK, "{\"Id\":7,\"Name\":\"response\"}");
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);
            CodecTestModel payload = new() { Id = 3, Name = "payload" };
            RestRequestResult<CodecTestModel> result = method == "POST"
                ? await client.PostAsync<CodecTestModel, CodecTestModel>(uri, payload)
                : await client.PutAsync<CodecTestModel, CodecTestModel>(uri, payload);

            Assert.AreEqual(method, capturedMethod);
            Assert.AreEqual("application/json", capturedMediaType);
            StringAssert.Contains(capturedBody, "\"Id\":3");
            StringAssert.Contains(capturedBody, "\"Name\":\"payload\"");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(7, result.Data?.Id);
            Assert.AreEqual("response", result.Data?.Name);
            Assert.IsTrue(result.IsSuccessful);
        }

        /// <summary>Verifies that request headers and authentication survive the centralized path.</summary>
        [TestMethod]
        public async Task HeaderOverloadsPreserveCustomAndAuthenticationHeaders()
        {
            string? authorization = null;
            string? customHeader = null;
            using RecordingMessageHandler handler = new((request, _) =>
            {
                authorization = request.Headers.Authorization?.ToString();
                customHeader = request.Headers.GetValues("X-History").Single();
                return Task.FromResult(CreateTextResponse(HttpStatusCode.OK, "ok"));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);
            RequestHeaders headers = new(new AuthenticationHeader("Bearer", "token"));
            headers.Headers.Add("X-History", "kept");

            RestRequestResult result = await client.GetAsync("https://example.test/headers", headers);

            Assert.AreEqual("Bearer token", authorization);
            Assert.AreEqual("kept", customHeader);
            Assert.IsTrue(result.IsSuccessful);
        }

        /// <summary>Verifies that buffered send failures remain results rather than escaping as exceptions.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SendFailureRemainsAnUnsuccessfulResult(bool typed)
        {
            HttpRequestException expected = new("network unavailable");
            using RecordingMessageHandler handler = new((_, _) => Task.FromException<HttpResponseMessage>(expected));
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult result = typed
                ? await client.GetAsync<string>("https://example.test/failure")
                : await client.GetAsync("https://example.test/failure");

            Assert.AreSame(expected, result.Exception);
            Assert.IsFalse(result.IsSuccessful);
            Assert.IsNull(result.StatusCode);
            Assert.IsTrue(result.Elapsed >= TimeSpan.Zero);
        }

        /// <summary>Verifies that buffered cancellation retains the historical result-based contract.</summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task CancellationRemainsAnUnsuccessfulResult(bool typed, bool cancelBeforeSend)
        {
            using CancellationTokenSource cancellationTokenSource = new();
            if (cancelBeforeSend)
                cancellationTokenSource.Cancel();
            using RecordingMessageHandler handler = new((_, cancellationToken) =>
            {
                cancellationTokenSource.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult result = typed
                ? await client.GetAsync<string>("https://example.test/cancelled", cancellationToken: cancellationTokenSource.Token)
                : await client.GetAsync("https://example.test/cancelled", cancellationTokenSource.Token);

            Assert.IsInstanceOfType<OperationCanceledException>(result.Exception);
            Assert.IsFalse(result.IsSuccessful);
            Assert.IsNull(result.StatusCode);
        }

        /// <summary>Verifies that finite responses are disposed after their content has been decoded.</summary>
        [TestMethod]
        public async Task FiniteResponseIsDisposedAfterDecode()
        {
            TrackingResponseContent content = new(Encoding.UTF8.GetBytes("historic"));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };
            using RecordingMessageHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            }));
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult<string> result = await client.GetAsync<string>("https://example.test/disposal");

            Assert.AreEqual("historic", result.Data);
            Assert.IsTrue(content.Disposed);
        }

        /// <summary>Verifies direct overload precedence between an explicit serializer and the client default.</summary>
        [TestMethod]
        [DataRow("GET")]
        [DataRow("DELETE")]
        [DataRow("POST")]
        [DataRow("PUT")]
        public async Task DirectTypedOverloadKeepsExplicitSerializerPrecedence(string method)
        {
            const string uri = "https://example.test/serializer";
            const PayloadJsonSerializerLibrary serializer = PayloadJsonSerializerLibrary.SystemTextJson;
            SerializerSelectionModel payload = new() { Name = "payload" };
            string? body = null;
            using RecordingMessageHandler handler = new(async (request, token) =>
            {
                body = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
                return CreateJsonResponse(HttpStatusCode.OK, "{\"system_name\":\"system\",\"newtonsoft_name\":\"newtonsoft\"}");
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context)
            {
                SelectedDefaultSerializationLibrary = PayloadJsonSerializerLibrary.NewtonsoftJson
            };

            RestRequestResult<SerializerSelectionModel> result = method switch
            {
                "GET" => await client.GetAsync<SerializerSelectionModel>(uri, serializer),
                "DELETE" => await client.DeleteAsync<SerializerSelectionModel>(uri, serializer),
                "POST" => await client.PostAsync<SerializerSelectionModel, SerializerSelectionModel>(uri, payload, serializer),
                "PUT" => await client.PutAsync<SerializerSelectionModel, SerializerSelectionModel>(uri, payload, serializer),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            Assert.AreEqual("system", result.Data?.Name);
            Assert.AreEqual(serializer, result.Request.ForcePayloadJsonSerializerLibrary);
            if (method is "POST" or "PUT")
                Assert.AreEqual("{\"system_name\":\"payload\"}", body);
        }

        /// <summary>Verifies the historical client-default precedence of typed header overloads.</summary>
        [TestMethod]
        [DataRow("GET")]
        [DataRow("DELETE")]
        [DataRow("POST")]
        [DataRow("PUT")]
        public async Task HeaderTypedOverloadKeepsClientDefaultSerializerPrecedence(string method)
        {
            const string uri = "https://example.test/serializer";
            const PayloadJsonSerializerLibrary serializer = PayloadJsonSerializerLibrary.SystemTextJson;
            SerializerSelectionModel payload = new() { Name = "payload" };
            RequestHeaders headers = new();
            string? body = null;
            using RecordingMessageHandler handler = new(async (request, token) =>
            {
                body = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
                return CreateJsonResponse(HttpStatusCode.OK, "{\"system_name\":\"system\",\"newtonsoft_name\":\"newtonsoft\"}");
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context)
            {
                SelectedDefaultSerializationLibrary = PayloadJsonSerializerLibrary.NewtonsoftJson
            };

            RestRequestResult<SerializerSelectionModel> result = method switch
            {
                "GET" => await client.GetAsync<SerializerSelectionModel>(uri, headers, serializer),
                "DELETE" => await client.DeleteAsync<SerializerSelectionModel>(uri, headers, serializer),
                "POST" => await client.PostAsync<SerializerSelectionModel, SerializerSelectionModel>(uri, payload, headers, serializer),
                "PUT" => await client.PutAsync<SerializerSelectionModel, SerializerSelectionModel>(uri, payload, headers, serializer),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            Assert.AreEqual("newtonsoft", result.Data?.Name);
            Assert.AreEqual(PayloadJsonSerializerLibrary.NewtonsoftJson, result.Request.ForcePayloadJsonSerializerLibrary);
            if (method is "POST" or "PUT")
                Assert.AreEqual("{\"system_name\":\"payload\"}", body);
        }

        /// <summary>Characterizes the legacy bodyless, typed-result behavior of untyped payload/header overloads.</summary>
        [TestMethod]
        [DataRow("POST")]
        [DataRow("PUT")]
        public async Task UntypedHeaderPayloadOverloadsRetainLegacyShape(string method)
        {
            bool hasContent = true;
            CodecTestModel payload = new() { Id = 3 };
            using RecordingMessageHandler handler = new((request, _) =>
            {
                hasContent = request.Content != null;
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"Id\":7}"));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult result = method == "POST"
                ? await client.PostAsync("https://example.test/legacy", payload, new RequestHeaders())
                : await client.PutAsync("https://example.test/legacy", payload, new RequestHeaders());

            Assert.IsFalse(hasContent);
            Assert.IsInstanceOfType<RestRequestResult<CodecTestModel>>(result);
            Assert.AreEqual(7, ((RestRequestResult<CodecTestModel>)result).Data?.Id);
        }

        /// <summary>Creates a deterministic JSON response.</summary>
        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string content)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        }

        /// <summary>Creates a deterministic text response.</summary>
        private static HttpResponseMessage CreateTextResponse(HttpStatusCode statusCode, string content)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/plain")
            };
        }

        #endregion
    }
}
