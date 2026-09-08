using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Multipart;
using AMDevIT.Restling.Tests.Cookies;
using AMDevIT.Restling.Tests.Multipart;
using AMDevIT.Restling.Tests.Proxy;
using System.Net;
using HttpMethod = AMDevIT.Restling.Core.HttpMethod;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class RequestProxyOverrideTests
    {
        #region Fields

        private static readonly string[] expected = ["http://127.0.0.1:1/raw", "http://127.0.0.1:1/form", "http://127.0.0.1:1/multipart"];

        #endregion

        #region Methods

        /// <summary>Default, custom proxy, and direct calls use independent routes and one shared cookie jar.</summary>
        [TestMethod]
        public async Task RoutingModesShareCookiesWithoutInterfering()
        {
            string responseWithCookie = LoopbackCookieServer.Response(200, "Set-Cookie: session=shared; Path=/");
            await using LoopbackCookieServer contextProxy = new(responseWithCookie);
            await using LoopbackCookieServer requestProxy = new(LoopbackCookieServer.Response(200));
            await using LoopbackCookieServer origin = new(LoopbackCookieServer.Response(200));
            HttpClientContextBuilder builder = new();
            builder.AddProxy(contextProxy.BaseUri.AbsoluteUri, false)
                   .AddDefaultHeader("X-Restling-Test", "preserved")
                   .SetTimeout(TimeSpan.FromSeconds(10));
            using HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);
            RestRequest defaultRequest = new(origin.BaseUri.AbsoluteUri, HttpMethod.Get);
            RestRequest customRequest = new(origin.BaseUri.AbsoluteUri, HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Custom(requestProxy.BaseUri.AbsoluteUri)
            };
            RestRequest directRequest = new(origin.BaseUri.AbsoluteUri, HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Direct()
            };

            Assert.IsTrue((await client.ExecuteRequestAsync(defaultRequest)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteRequestAsync(customRequest)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteRequestAsync(directRequest)).IsSuccessful);
            IReadOnlyList<LoopbackCookieServer.Request> contextRequests = await contextProxy.Requests;
            IReadOnlyList<LoopbackCookieServer.Request> customRequests = await requestProxy.Requests;
            IReadOnlyList<LoopbackCookieServer.Request> originRequests = await origin.Requests;

            Assert.AreEqual(origin.BaseUri.AbsoluteUri, contextRequests[0].Target);
            Assert.AreEqual(origin.BaseUri.AbsoluteUri, customRequests[0].Target);
            Assert.AreEqual("/", originRequests[0].Target);
            StringAssert.Contains(customRequests[0].Headers["Cookie"], "session=shared");
            StringAssert.Contains(originRequests[0].Headers["Cookie"], "session=shared");
            Assert.AreEqual("preserved", customRequests[0].Headers["X-Restling-Test"]);
            Assert.AreEqual("preserved", originRequests[0].Headers["X-Restling-Test"]);
            Assert.AreSame(context.CookieContainer, ((SocketsHttpHandler)context.HttpMessageHandler).CookieContainer);
        }

        /// <summary>Equivalent overrides reuse their handler while different redirect policies use separate transports.</summary>
        [TestMethod]
        public async Task EquivalentOptionsReuseAlternativeTransport()
        {
            int factoryCalls = 0;
            await using LoopbackCookieServer proxy = new(LoopbackCookieServer.Response(200),
                                                         LoopbackCookieServer.Response(200),
                                                         LoopbackCookieServer.Response(200));
            using RecordingMessageHandler defaultHandler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            HttpClientContextBuilder builder = new();
            builder.AddHandler(defaultHandler).AddRequestHandlerFactory(cookies =>
            {
                Interlocked.Increment(ref factoryCalls);
                return new SocketsHttpHandler { CookieContainer = cookies };
            });
            using HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);
            RequestProxyOptions firstOptions = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri, false);
            RequestProxyOptions equivalentOptions = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri, false);
            RestRequest first = new("http://127.0.0.1:1/one", HttpMethod.Get) { ProxyOptions = firstOptions };
            RestRequest second = new("http://127.0.0.1:1/two", HttpMethod.Get) { ProxyOptions = equivalentOptions };
            RestRequest third = new("http://127.0.0.1:1/three", HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri, true)
            };

            Assert.IsTrue((await client.ExecuteRequestAsync(first)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteRequestAsync(second)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteRequestAsync(third)).IsSuccessful);
            await proxy.Requests;
            Assert.AreEqual(2, factoryCalls);
        }

        /// <summary>A truncated response evicts the failed alternative transport before the caller retries.</summary>
        [TestMethod]
        public async Task ResponseEndedInvalidatesAlternativeTransport()
        {
            int factoryCalls = 0;
            string truncatedResponse = "HTTP/1.1 200 Test\r\nContent-Type: application/octet-stream\r\n" +
                                       "Content-Length: 10\r\nConnection: close\r\n\r\nshort";
            await using LoopbackCookieServer proxy = new(truncatedResponse, LoopbackCookieServer.Response(200));
            using RecordingMessageHandler defaultHandler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            List<TrackingHttpClientHandler> alternativeHandlers = [];
            HttpClientContextBuilder builder = new();
            builder.AddHandler(defaultHandler).AddRequestHandlerFactory(cookies =>
            {
                TrackingHttpClientHandler handler = new() { CookieContainer = cookies };
                factoryCalls++;
                alternativeHandlers.Add(handler);
                return handler;
            });
            using HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);
            RequestProxyOptions options = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri);
            RestRequest firstRequest = new("http://127.0.0.1:1/first", HttpMethod.Get) { ProxyOptions = options };
            RestRequest secondRequest = new("http://127.0.0.1:1/second", HttpMethod.Get) { ProxyOptions = options };

            RestRequestResult firstResult = await client.ExecuteRequestAsync(firstRequest);
            RestRequestResult secondResult = await client.ExecuteRequestAsync(secondRequest);
            await proxy.Requests;

            Assert.IsFalse(firstResult.IsSuccessful);
            Assert.IsInstanceOfType<HttpRequestException>(firstResult.Exception);
            Assert.IsTrue(secondResult.IsSuccessful);
            Assert.AreEqual(2, factoryCalls);
            Assert.AreEqual(2, alternativeHandlers.Count);
            Assert.IsTrue(alternativeHandlers[0].Disposed);
            Assert.IsFalse(alternativeHandlers[1].Disposed);
        }

        /// <summary>Each custom override independently controls automatic HTTP redirects.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CustomOverrideControlsRedirects(bool allowAutoRedirect)
        {
            string start = "http://127.0.0.1:1/start";
            string next = "http://127.0.0.1:1/next";
            string redirect = LoopbackCookieServer.Response(302, $"Location: {next}");
            string[] responses = allowAutoRedirect
                ? [redirect, LoopbackCookieServer.Response(200)]
                : [redirect];
            await using LoopbackCookieServer proxy = new(responses);
            using HttpClientContext context = new HttpClientContextBuilder().Build();
            using RestlingClient client = new(context);
            RestRequest request = new(start, HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri, allowAutoRedirect)
            };

            RestRequestResult result = await client.ExecuteRequestAsync(request);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await proxy.Requests;

            Assert.AreEqual(allowAutoRedirect ? HttpStatusCode.OK : HttpStatusCode.Found, result.StatusCode);
            Assert.AreEqual(allowAutoRedirect ? 2 : 1, requests.Count);
            Assert.AreEqual(start, requests[0].Target);
            if (allowAutoRedirect)
                Assert.AreEqual(next, requests[1].Target);
        }

        /// <summary>A supplied handler requires an explicit alternative-handler factory only when an override is used.</summary>
        [TestMethod]
        public async Task MissingFactoryDoesNotAffectDefaultRequests()
        {
            using RecordingMessageHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            using RestlingClient client = new(context);
            RestRequest defaultRequest = new("http://example.test/default", HttpMethod.Get);
            RestRequest directRequest = new("http://example.test/direct", HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Direct()
            };

            Assert.IsTrue((await client.ExecuteRequestAsync(defaultRequest)).IsSuccessful);
            RestRequestResult result = await client.ExecuteRequestAsync(directRequest);
            Assert.IsInstanceOfType<NotSupportedException>(result.Exception);
        }

        /// <summary>A bad factory becomes a normal buffered request failure instead of silently bypassing routing.</summary>
        [TestMethod]
        public async Task UnsupportedFactoryHandlerReturnsFailure()
        {
            using RecordingMessageHandler defaultHandler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            RecordingMessageHandler? alternative = null;
            HttpClientContextBuilder builder = new();
            builder.AddHandler(defaultHandler).AddRequestHandlerFactory(_ =>
            {
                alternative = new RecordingMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
                return alternative;
            });
            using HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);
            RestRequest request = new("http://example.test/direct", HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Direct()
            };

            RestRequestResult result = await client.ExecuteRequestAsync(request);
            Assert.IsInstanceOfType<NotSupportedException>(result.Exception);
            Assert.IsNotNull(alternative);
        }

        /// <summary>The context owns and disposes handlers created for request overrides.</summary>
        [TestMethod]
        public async Task ContextDisposesAlternativeHandlers()
        {
            TrackingHttpClientHandler? alternative = null;
            await using LoopbackCookieServer origin = new(LoopbackCookieServer.Response(200));
            using RecordingMessageHandler defaultHandler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            HttpClientContextBuilder builder = new();
            builder.AddHandler(defaultHandler).AddRequestHandlerFactory(cookies =>
            {
                alternative = new TrackingHttpClientHandler { CookieContainer = cookies };
                return alternative;
            });
            HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);
            RestRequest request = new(origin.BaseUri.AbsoluteUri, HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Direct()
            };

            Assert.IsTrue((await client.ExecuteRequestAsync(request)).IsSuccessful);
            await origin.Requests;
            Assert.IsNotNull(alternative);
            Assert.IsFalse(alternative.Disposed);
            context.Dispose();
            Assert.IsTrue(alternative.Disposed);
        }

        /// <summary>Credentials seeded by a factory are transferred to the request-selected proxy address.</summary>
        [TestMethod]
        public void FactoryProxyCredentialsArePreserved()
        {
            NetworkCredential credentials = new("request-user", "test-password");
            SocketsHttpHandler? alternative = null;
            using RecordingMessageHandler defaultHandler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            HttpClientContextBuilder builder = new();
            builder.AddHandler(defaultHandler).AddRequestHandlerFactory(cookies =>
            {
                alternative = new SocketsHttpHandler
                {
                    CookieContainer = cookies,
                    Proxy = new WebProxy("http://placeholder.test") { Credentials = credentials }
                };
                return alternative;
            });
            using HttpClientContext context = builder.Build();
            RequestProxyOptions options = RequestProxyOptions.Custom("http://localhost:8080");

            context.ResolveHttpClient(options);

            Assert.IsNotNull(alternative);
            Assert.IsInstanceOfType<WebProxy>(alternative.Proxy);
            Assert.AreEqual(new Uri("http://localhost:8080/"), ((WebProxy)alternative.Proxy).Address);
            Assert.AreSame(credentials, alternative.Proxy.Credentials);
        }        

        /// <summary>Raw, form, and buffered multipart executions all honor the inherited request override.</summary>
        [TestMethod]
        public async Task SpecializedBufferedRequestsUseSelectedProxy()
        {
            await using LoopbackCookieServer proxy = new(LoopbackCookieServer.Response(200),
                                                         LoopbackCookieServer.Response(200),
                                                         LoopbackCookieServer.Response(200));
            using HttpClientContext context = new HttpClientContextBuilder().Build();
            using RestlingClient client = new(context);
            RequestProxyOptions options = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri);
            RestRawRequest raw = new("http://127.0.0.1:1/raw", HttpMethod.Post, "raw") { ProxyOptions = options };
            FormUrlEncodedRequest form = new("http://127.0.0.1:1/form", HttpMethod.Post,
                                             new Dictionary<string, string> { ["name"] = "value" })
            {
                ProxyOptions = options
            };
            MultipartRequest multipart = new("http://127.0.0.1:1/multipart", HttpMethod.Post)
            {
                ProxyOptions = options
            };
            multipart.AddText("name", "value");

            Assert.IsTrue((await client.ExecuteRawRequestAsync(raw)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteFormUrlEncodedRequest(form)).IsSuccessful);
            Assert.IsTrue((await client.ExecuteMultipartRequestAsync(multipart)).IsSuccessful);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await proxy.Requests;
            CollectionAssert.AreEqual(expected,
                                      requests.Select(request => request.Target).ToArray());
        }

        /// <summary>Mixed-replace streaming keeps the selected transport alive through enumeration.</summary>
        [TestMethod]
        public async Task StreamingRequestUsesSelectedProxy()
        {
            string body = "--frame\r\nContent-Type: text/plain\r\n\r\none\r\n--frame--\r\n";
            string response = LoopbackCookieServer.ResponseWithBody(200,
                                                                    body,
                                                                    "multipart/x-mixed-replace; boundary=frame");
            await using LoopbackCookieServer proxy = new(response);
            using HttpClientContext context = new HttpClientContextBuilder().Build();
            using RestlingClient client = new(context);
            RestRequest request = new("http://127.0.0.1:1/stream", HttpMethod.Get)
            {
                ProxyOptions = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri)
            };
            List<MultipartPart> parts = [];

            await foreach (MultipartPart part in client.StreamMultipartMixedReplaceAsync(request))
                parts.Add(part);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await proxy.Requests;

            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("one", parts[0].Deserialize<string>());
            Assert.AreEqual("http://127.0.0.1:1/stream", requests[0].Target);
        }

        /// <summary>Request proxy values validate schemes and remain immutable value objects.</summary>
        [TestMethod]
        [DataRow("http")]
        [DataRow("https")]
        [DataRow("socks4")]
        [DataRow("socks4a")]
        [DataRow("socks5")]
        public void CustomOptionsAcceptSupportedSchemes(string scheme)
        {
            RequestProxyOptions options = RequestProxyOptions.Custom($"{scheme}://localhost:8080", true);
            Assert.AreEqual(RequestProxyMode.Custom, options.Mode);
            Assert.AreEqual(new Uri($"{scheme}://localhost:8080/"), options.ProxyUri);
            Assert.IsTrue(options.AllowAutoRedirect);
        }

        /// <summary>Direct options never carry a proxy address.</summary>
        [TestMethod]
        public void DirectOptionsAreExplicitAndValueComparable()
        {
            Assert.AreEqual(RequestProxyOptions.Direct(true), RequestProxyOptions.Direct(true));
            Assert.AreEqual(RequestProxyMode.Direct, RequestProxyOptions.Direct().Mode);
            Assert.IsNull(RequestProxyOptions.Direct().ProxyUri);
            Assert.AreSame(RequestProxyOptions.Default, RequestProxyOptions.Default);
        }

        /// <summary>Every direct convenience family exposes proxy options with CancellationToken last.</summary>
        [TestMethod]
        public async Task DirectConvenienceOverloadsRouteThroughSelectedProxy()
        {
            string[] responses = Enumerable.Repeat(LoopbackCookieServer.Response(200), 16).ToArray();
            await using LoopbackCookieServer proxy = new(responses);
            using HttpClientContext context = new HttpClientContextBuilder().Build();
            using RestlingClient concreteClient = new(context);
            IRestlingClient client = concreteClient;
            RequestProxyOptions options = RequestProxyOptions.Custom(proxy.BaseUri.AbsoluteUri);
            RequestHeaders headers = new();
            CancellationToken cancellationToken = CancellationToken.None;
            List<bool> successful =
            [
                (await client.GetAsync("http://127.0.0.1:1/get", options, cancellationToken)).IsSuccessful,
                (await client.GetAsync("http://127.0.0.1:1/get-headers", headers, options, cancellationToken)).IsSuccessful,
                (await client.GetAsync<string>("http://127.0.0.1:1/get-typed", null, options, cancellationToken)).IsSuccessful,
                (await client.GetAsync<string>("http://127.0.0.1:1/get-typed-headers", headers, null, options, cancellationToken)).IsSuccessful,
                (await client.PostAsync("http://127.0.0.1:1/post", "data", null, options, cancellationToken)).IsSuccessful,
                (await client.PostAsync("http://127.0.0.1:1/post-headers", "data", headers, null, options, cancellationToken)).IsSuccessful,
                (await client.PostAsync<string, string>("http://127.0.0.1:1/post-typed", "data", null, options, cancellationToken)).IsSuccessful,
                (await client.PostAsync<string, string>("http://127.0.0.1:1/post-typed-headers", "data", headers, null, options, cancellationToken)).IsSuccessful,
                (await client.PutAsync("http://127.0.0.1:1/put", "data", null, options, cancellationToken)).IsSuccessful,
                (await client.PutAsync("http://127.0.0.1:1/put-headers", "data", headers, null, options, cancellationToken)).IsSuccessful,
                (await client.PutAsync<string, string>("http://127.0.0.1:1/put-typed", "data", null, options, cancellationToken)).IsSuccessful,
                (await client.PutAsync<string, string>("http://127.0.0.1:1/put-typed-headers", "data", headers, null, options, cancellationToken)).IsSuccessful,
                (await client.DeleteAsync("http://127.0.0.1:1/delete", options, cancellationToken)).IsSuccessful,
                (await client.DeleteAsync("http://127.0.0.1:1/delete-headers", headers, options, cancellationToken)).IsSuccessful,
                (await client.DeleteAsync<string>("http://127.0.0.1:1/delete-typed", null, options, cancellationToken)).IsSuccessful,
                (await client.DeleteAsync<string>("http://127.0.0.1:1/delete-typed-headers", headers, null, options, cancellationToken)).IsSuccessful
            ];
            IReadOnlyList<LoopbackCookieServer.Request> requests = await proxy.Requests;

            Assert.IsTrue(successful.All(value => value));
            Assert.AreEqual(16, requests.Count);
            Assert.IsTrue(requests.All(request => request.Target.StartsWith("http://127.0.0.1:1/", StringComparison.Ordinal)));
            IEnumerable<System.Reflection.MethodInfo> overloads = typeof(IRestlingClient).GetMethods()
                .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(RequestProxyOptions)));
            Assert.AreEqual(16, overloads.Count());
            Assert.IsTrue(overloads.All(method => method.GetParameters()[^1].ParameterType == typeof(CancellationToken)));
        }

        #endregion
    }
}
