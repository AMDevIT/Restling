using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Multipart;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Tests.Models;
using AMDevIT.Restling.Tests.Multipart;
using AMDevIT.Restling.Tests.Pipeline;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using RestlingHttpMethod = AMDevIT.Restling.Core.HttpMethod;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class HttpPipelineEdgeCaseTests
    {
        #region Methods

        /// <summary>Retains empty text content for null payloads in all direct POST and PUT overloads.</summary>
        [TestMethod]
        [DataRow("POST", false)]
        [DataRow("POST", true)]
        [DataRow("PUT", false)]
        [DataRow("PUT", true)]
        public async Task NullDirectPayloadRetainsEmptyTextContent(string method, bool typed)
        {
            string? body = null;
            string? mediaType = null;
            using RecordingMessageHandler handler = new(async (request, token) =>
            {
                mediaType = request.Content?.Headers.ContentType?.ToString();
                body = await request.Content!.ReadAsStringAsync(token);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult result = await ExecuteDirectPayloadAsync<string?>(client, method, typed, null);

            Assert.AreEqual(string.Empty, body);
            Assert.AreEqual("text/plain; charset=utf-8", mediaType);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
            Assert.IsTrue(result.IsSuccessful);
        }

        /// <summary>Retains result-based serialization errors without issuing a network request.</summary>
        [TestMethod]
        [DataRow("POST", false)]
        [DataRow("POST", true)]
        [DataRow("PUT", false)]
        [DataRow("PUT", true)]
        public async Task DirectSerializationFailureRemainsAResult(string method, bool typed)
        {
            int sends = 0;
            Dictionary<string, object> cycle = [];
            cycle["self"] = cycle;
            using RecordingMessageHandler handler = new((_, _) =>
            {
                sends++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult result = await ExecuteDirectPayloadAsync(client, method, typed, cycle);

            Assert.AreEqual(0, sends);
            Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
            Assert.IsFalse(result.IsSuccessful);
            Assert.IsNull(result.StatusCode);
            Assert.AreEqual(TimeSpan.Zero, result.Elapsed);
        }

        /// <summary>Retains raw/form payloads and their independent response serializer selection.</summary>
        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, false)]
        [DataRow(false, true, true)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(true, true, false)]
        [DataRow(true, true, true)]
        public async Task RawAndFormPreserveBodyAndSerializer(bool form, bool typed, bool forceSystem)
        {
            const string uri = "https://example.test/resource";
            string? body = null;
            string? mediaType = null;
            PayloadJsonSerializerLibrary? serializer = forceSystem ? PayloadJsonSerializerLibrary.SystemTextJson : null;
            RestRequest request = form
                ? new FormUrlEncodedRequest(uri, RestlingHttpMethod.Post, new Dictionary<string, string> { ["q"] = "a b&c" })
                : new RestRawRequest(uri, RestlingHttpMethod.Post, content: "a b&c", contentType: "text/plain");
            request.ForcePayloadJsonSerializerLibrary = serializer;
            using RecordingMessageHandler handler = new(async (message, token) =>
            {
                body = await message.Content!.ReadAsStringAsync(token);
                mediaType = message.Content.Headers.ContentType?.MediaType;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"system_name\":\"system\",\"newtonsoft_name\":\"newtonsoft\"}", Encoding.UTF8, "application/json")
                };
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context)
            {
                SelectedDefaultSerializationLibrary = forceSystem
                    ? PayloadJsonSerializerLibrary.NewtonsoftJson
                    : PayloadJsonSerializerLibrary.SystemTextJson
            };

            RestRequestResult result = typed
                ? await client.ExecuteRequestAsync<SerializerSelectionModel>(request)
                : await client.ExecuteRequestAsync(request);

            Assert.AreEqual(form ? "q=a+b%26c" : "a b&c", body);
            Assert.AreEqual(form ? "application/x-www-form-urlencoded" : "text/plain", mediaType);
            Assert.AreSame(request, result.Request);
            Assert.AreEqual(serializer, request.ForcePayloadJsonSerializerLibrary);
            Assert.IsTrue(result.IsSuccessful);
            if (typed)
                Assert.AreEqual(forceSystem ? "system" : "newtonsoft", ((RestRequestResult<SerializerSelectionModel>)result).Data?.Name);
        }

        /// <summary>Retains legacy JSON default-on-error and status-dependent XML decode errors.</summary>
        [TestMethod]
        [DataRow("application/json", HttpStatusCode.OK, false)]
        [DataRow("application/json", HttpStatusCode.BadRequest, false)]
        [DataRow("application/xml", HttpStatusCode.OK, true)]
        [DataRow("application/xml", HttpStatusCode.BadRequest, false)]
        public async Task DecodeErrorsPreserveStatusBodyAndDisposal(string mediaType, HttpStatusCode statusCode, bool hasException)
        {
            byte[] bytes = Encoding.UTF8.GetBytes("invalid document");
            TrackingResponseContent content = new(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            using RecordingMessageHandler handler = new((_, _) =>
            {
                HttpResponseMessage response = new(statusCode) { Content = content };
                response.Headers.Add("X-Response", "kept");
                return Task.FromResult(response);
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            RestRequestResult<CodecTestModel> result = await client.GetAsync<CodecTestModel>("https://example.test/decode");

            Assert.AreEqual(statusCode, result.StatusCode);
            Assert.AreEqual(hasException, result.Exception != null);
            Assert.AreEqual(statusCode == HttpStatusCode.OK && !hasException, result.IsSuccessful);
            Assert.IsNull(result.Data);
            CollectionAssert.AreEqual(bytes, result.RawContent);
            Assert.AreEqual("kept", result.ResponseHeaders.Headers["X-Response"].Single());
            Assert.IsTrue(content.Disposed);
        }

        /// <summary>Preserves direct shortcut HTTP version defaults, unlike explicitly constructed requests.</summary>
        [TestMethod]
        [DataRow("GET")]
        [DataRow("DELETE")]
        [DataRow("POST")]
        [DataRow("PUT")]
        public async Task DirectMethodsPreserveHttpClientVersionDefaults(string method)
        {
            Version? version = null;
            HttpVersionPolicy? policy = null;
            using RecordingMessageHandler handler = new((request, _) =>
            {
                version = request.Version;
                policy = request.VersionPolicy;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            context.HttpClient.DefaultRequestVersion = HttpVersion.Version20;
            context.HttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            using RestlingClient client = new(context);

            RestRequestResult result = method switch
            {
                "GET" => await client.GetAsync("https://example.test/version"),
                "DELETE" => await client.DeleteAsync("https://example.test/version"),
                _ => await ExecuteDirectPayloadAsync(client, method, false, "payload")
            };

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(HttpVersion.Version20, version);
            Assert.AreEqual(HttpVersionPolicy.RequestVersionExact, policy);
        }

        /// <summary>Verifies header-only completion and response disposal when streaming is stopped early.</summary>
        [TestMethod]
        public async Task StreamingRemainsUnbufferedAndDisposesOnEarlyBreak()
        {
            const string body = "--frame\r\nContent-Type: text/plain\r\n\r\none\r\n--frame\r\nContent-Type: text/plain\r\n\r\ntwo\r\n--frame--\r\n";
            TrackingResponseContent content = new(Encoding.UTF8.GetBytes(body));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/x-mixed-replace; boundary=frame");
            using RecordingMessageHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            context.HttpClient.MaxResponseContentBufferSize = 1;
            using RestlingClient client = new(context);
            RestRequest request = new("https://example.test/stream", RestlingHttpMethod.Get);
            string? first = null;

            await foreach (MultipartPart part in client.StreamMultipartMixedReplaceAsync(request))
            {
                first = part.Deserialize<string>();
                break;
            }

            Assert.AreEqual("one", first);
            Assert.IsTrue(content.Disposed);
        }

        /// <summary>Preserves thrown preparation failures for explicitly constructed payload requests.</summary>
        [TestMethod]
        public async Task ExplicitPayloadRequestRetainsThrownSerializationError()
        {
            int sends = 0;
            Exception? failure = null;
            Dictionary<string, object> cycle = [];
            cycle["self"] = cycle;
            RestRequest<Dictionary<string, object>> request = new("https://example.test/resource", RestlingHttpMethod.Post, cycle);
            using RecordingMessageHandler handler = new((_, _) =>
            {
                sends++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);

            try
            {
                await client.ExecuteRequestAsync<string, Dictionary<string, object>>(request);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.IsInstanceOfType<InvalidOperationException>(failure);
            Assert.AreEqual(0, sends);
        }

        /// <summary>Preserves exception propagation for the dedicated streaming API.</summary>
        [TestMethod]
        public async Task StreamingSendFailureStillThrows()
        {
            HttpRequestException expected = new("stream unavailable");
            Exception? failure = null;
            using RecordingMessageHandler handler = new((_, _) => Task.FromException<HttpResponseMessage>(expected));
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);
            RestRequest request = new("https://example.test/stream", RestlingHttpMethod.Get);

            try
            {
                await foreach (MultipartPart part in client.StreamMultipartMixedReplaceAsync(request))
                    Assert.Fail("A failed stream must not yield a part.");
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.AreSame(expected, failure);
        }

        /// <summary>Exercises all direct payload overloads with the same test data.</summary>
        private static async Task<RestRequestResult> ExecuteDirectPayloadAsync<T>(RestlingClient client, string method, bool typed, T payload)
        {
            const string uri = "https://example.test/resource";
            return (method, typed) switch
            {
                ("POST", false) => await client.PostAsync(uri, payload),
                ("POST", true) => await client.PostAsync<string, T>(uri, payload),
                ("PUT", false) => await client.PutAsync(uri, payload),
                ("PUT", true) => await client.PutAsync<string, T>(uri, payload),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };
        }

        #endregion
    }
}
