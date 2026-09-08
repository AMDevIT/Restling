using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Tests.Cookies;
using AMDevIT.Restling.Tests.Storage;
using System.Net;
using System.Text;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class CookiePersistenceTests
    {
        #region Methods

        /// <summary>The pipeline loads an attached provider once and reports each received response.</summary>
        [TestMethod]
        public async Task AttachedStorageIsLoadedAndNotifiedAsync()
        {
            TrackingCookieStorageProvider storage = new();
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(200,
                                                                                        "Set-Cookie: persisted=yes; Path=/"));
            HttpClientContextBuilder builder = new();
            builder.AddCookieStorageProvider(storage)
                   .ConfigureHandler(handler => ((SocketsHttpHandler)handler).UseProxy = false)
                   .SetTimeout(TimeSpan.FromSeconds(10));
            using HttpClientContext context = builder.Build();
            using RestlingClient client = new(context);

            RestRequestResult result = await client.GetAsync(server.BaseUri.AbsoluteUri);

            Assert.IsTrue(result.IsSuccessful, result.Exception?.ToString());
            Assert.AreEqual(1, storage.LoadCount);
            Assert.AreEqual(1, storage.NotificationCount);
            Assert.AreEqual("yes", storage.CookieContainer.GetCookies(server.BaseUri)["persisted"]?.Value);
        }

        /// <summary>Verifies seeded and response cookies across POST/PUT calls and recreated clients.</summary>
        [TestMethod]
        [DataRow("default", "POST")]
        [DataRow("default", "PUT")]
        [DataRow("sockets", "POST")]
        [DataRow("sockets", "PUT")]
        [DataRow("http-client", "POST")]
        [DataRow("http-client", "PUT")]
        [DataRow("configured", "POST")]
        [DataRow("configured", "PUT")]
        public async Task CookiesSurviveConsecutiveCallsAndClientRecreation(string mode, string method)
        {
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(200, "Set-Cookie: session=first; Path=/; HttpOnly"),
                                                         LoopbackCookieServer.Response(200, "Set-Cookie: session=second; Path=/; HttpOnly"),
                                                         LoopbackCookieServer.Response(200));
            using HttpClientContext context = CreateContext(mode);
            context.HttpClient.Timeout = TimeSpan.FromSeconds(10);
            using (RestlingClient firstClient = new(context))
            {
                RestRequestResult initial = await firstClient.GetAsync(new Uri(server.BaseUri, "start").AbsoluteUri);
                Assert.IsTrue(initial.IsSuccessful, initial.Exception?.ToString());
            }

            using RestlingClient nextClient = new(context);
            RequestHeaders headers = new();
            headers.Headers.Add("X-Call", "next");
            RestRequestResult next = method == "POST"
                ? await nextClient.PostAsync(new Uri(server.BaseUri, "next").AbsoluteUri, "payload", headers)
                : await nextClient.PutAsync(new Uri(server.BaseUri, "next").AbsoluteUri, "payload", headers);
            RestRequestResult last = await nextClient.GetAsync(new Uri(server.BaseUri, "last").AbsoluteUri);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await server.Requests;

            Assert.IsTrue(next.IsSuccessful, next.Exception?.ToString());
            Assert.IsTrue(last.IsSuccessful, last.Exception?.ToString());
            StringAssert.Contains(CookieHeader(requests[0]), "seed=configured");
            StringAssert.Contains(CookieHeader(requests[1]), "session=first");
            StringAssert.Contains(CookieHeader(requests[2]), "session=second");
            StringAssert.Contains(CookieHeader(requests[2]), "seed=configured");
            Assert.AreEqual(method, requests[1].Method);
            Assert.AreEqual("next", requests[1].Headers["X-Call"]);
            Assert.AreEqual("\"payload\"", Encoding.UTF8.GetString(requests[1].Body));
            Assert.AreEqual("second", context.CookieContainer.GetCookies(server.BaseUri)["session"]?.Value);
        }

        /// <summary>Verifies that cookies from a redirect are stored before a caller follows its location.</summary>
        [TestMethod]
        [DataRow(301)]
        [DataRow(302)]
        [DataRow(303)]
        [DataRow(307)]
        [DataRow(308)]
        public async Task ManualRedirectPreservesResponseCookies(int statusCode)
        {
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(statusCode,
                                                                                      "Location: /follow",
                                                                                      "Set-Cookie: redirected=yes; Path=/"),
                                                         LoopbackCookieServer.Response(200));
            using HttpClientContext context = CreateContext("default");
            using RestlingClient client = new(context);

            RestRequestResult redirect = await client.GetAsync(server.BaseUri.AbsoluteUri);

            Assert.AreEqual((HttpStatusCode)statusCode, redirect.StatusCode);
            Assert.AreEqual("/follow", redirect.ResponseHeaders.RedirectLocation);
            Assert.AreEqual("yes", context.CookieContainer.GetCookies(server.BaseUri)["redirected"]?.Value);

            RestRequestResult followed = await client.GetAsync(new Uri(server.BaseUri, redirect.ResponseHeaders.RedirectLocation!).AbsoluteUri);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await server.Requests;

            Assert.IsTrue(followed.IsSuccessful, followed.Exception?.ToString());
            Assert.AreEqual("/follow", requests[1].Target);
            StringAssert.Contains(CookieHeader(requests[1]), "redirected=yes");
        }

        /// <summary>Verifies cookie storage at every automatic redirect hop regardless of builder call order.</summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task AutomaticRedirectSharesExplicitCookieContainer(bool useHttpClientHandler, bool containerFirst)
        {
            CookieContainer cookies = new();
            HttpClientContextBuilder builder = new();
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(302, "Location: /follow", "Set-Cookie: hop=redirect; Path=/"),
                                                         LoopbackCookieServer.Response(200, "Set-Cookie: final=received; Path=/"),
                                                         LoopbackCookieServer.Response(200));
            cookies.Add(server.BaseUri, new Cookie("seed", "shared", "/"));
            using HttpMessageHandler handler = useHttpClientHandler
                ? new HttpClientHandler { AllowAutoRedirect = true, UseProxy = false }
                : new SocketsHttpHandler { AllowAutoRedirect = true, UseProxy = false };
            if (containerFirst)
                builder.AddCookieContainer(cookies).AddHandler(handler);
            else
                builder.AddHandler(handler).AddCookieContainer(cookies);
            using HttpClientContext context = builder.Build();
            context.HttpClient.Timeout = TimeSpan.FromSeconds(10);
            using RestlingClient client = new(context);

            RestRequestResult redirected = await client.GetAsync(server.BaseUri.AbsoluteUri);
            RestRequestResult subsequent = await client.GetAsync(new Uri(server.BaseUri, "last").AbsoluteUri);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await server.Requests;

            Assert.IsTrue(redirected.IsSuccessful, redirected.Exception?.ToString());
            Assert.IsTrue(subsequent.IsSuccessful, subsequent.Exception?.ToString());
            Assert.AreEqual("/follow", requests[1].Target);
            StringAssert.Contains(CookieHeader(requests[0]), "seed=shared");
            StringAssert.Contains(CookieHeader(requests[1]), "hop=redirect");
            StringAssert.Contains(CookieHeader(requests[2]), "final=received");
            StringAssert.Contains(CookieHeader(requests[2]), "hop=redirect");
            Assert.AreSame(cookies, context.CookieContainer);
            Assert.AreEqual("redirect", cookies.GetCookies(server.BaseUri)["hop"]?.Value);
            Assert.AreEqual("received", cookies.GetCookies(server.BaseUri)["final"]?.Value);
        }

        /// <summary>Verifies that native cookie domain/path/security and deletion rules are not bypassed.</summary>
        [TestMethod]
        public async Task CookieScopeAndDeletionAreRespected()
        {
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(200,
                                                                                      "Set-Cookie: private=restricted; Path=/private",
                                                                                      "Set-Cookie: secure=secret; Path=/; Secure",
                                                                                      "Set-Cookie: session=temporary; Path=/"),
                                                         LoopbackCookieServer.Response(200, "Set-Cookie: session=deleted; Path=/; Max-Age=0"),
                                                         LoopbackCookieServer.Response(200));
            using HttpClientContext context = CreateContext("default");
            context.CookieContainer.Add(new Uri("http://unrelated.test/"), new Cookie("foreign", "secret", "/"));
            using RestlingClient client = new(context);

            Assert.IsTrue((await client.GetAsync(server.BaseUri.AbsoluteUri)).IsSuccessful);
            Assert.IsTrue((await client.GetAsync(new Uri(server.BaseUri, "public").AbsoluteUri)).IsSuccessful);
            Assert.IsTrue((await client.GetAsync(new Uri(server.BaseUri, "private/resource").AbsoluteUri)).IsSuccessful);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await server.Requests;

            StringAssert.Contains(CookieHeader(requests[1]), "session=temporary");
            Assert.IsFalse(CookieHeader(requests[1]).Contains("private=", StringComparison.Ordinal));
            StringAssert.Contains(CookieHeader(requests[2]), "private=restricted");
            Assert.IsFalse(CookieHeader(requests[2]).Contains("session=", StringComparison.Ordinal));
            foreach (LoopbackCookieServer.Request request in requests)
            {
                Assert.IsFalse(CookieHeader(request).Contains("secure=", StringComparison.Ordinal));
                Assert.IsFalse(CookieHeader(request).Contains("foreign=", StringComparison.Ordinal));
            }
        }

        /// <summary>Creates a seeded context using the supported handler construction paths.</summary>
        private static HttpClientContext CreateContext(string mode)
        {
            HttpClientContextBuilder builder = new();
            builder.AddCookie(new HttpCookieData("seed", "configured", "127.0.0.1", "/"));
            switch (mode)
            {
                case "sockets":
                    builder.AddHandler(new SocketsHttpHandler { UseProxy = false }, HttpMessageHandlerOwnership.Owned);
                    break;
                case "http-client":
                    builder.AddHandler(new HttpClientHandler { UseProxy = false }, HttpMessageHandlerOwnership.Owned);
                    break;
                case "configured":
                    builder.ConfigureHandler(handler => ((SocketsHttpHandler)handler).UseProxy = false);
                    break;
            }

            HttpClientContext context = builder.Build();
            if (mode == "default")
                ((SocketsHttpHandler)context.HttpMessageHandler).UseProxy = false;
            context.HttpClient.Timeout = TimeSpan.FromSeconds(10);
            return context;
        }

        /// <summary>Returns the cookie header observed by the real loopback server.</summary>
        private static string CookieHeader(LoopbackCookieServer.Request request)
        {
            return request.Headers.TryGetValue("Cookie", out string? value) ? value : string.Empty;
        }

        #endregion
    }
}
