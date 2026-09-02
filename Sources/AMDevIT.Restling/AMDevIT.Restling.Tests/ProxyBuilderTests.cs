using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Tests.Cookies;
using AMDevIT.Restling.Tests.Multipart;
using System.Net;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class ProxyBuilderTests
    {
        #region Methods

        /// <summary>The interface exposes proxy configuration and internally created handlers remain owned.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DefaultHandlerUsesExplicitProxy(bool allowAutoRedirect)
        {
            IHttpClientContextBuilder builder = new HttpClientContextBuilder();
            Assert.AreSame(builder, builder.AddProxy("http://localhost:8080", allowAutoRedirect));
            using HttpClientContext context = builder.Build();

            AssertProxy(context.HttpMessageHandler, "http://localhost:8080/", allowAutoRedirect);
            Assert.AreEqual(HttpClientContextOwnership.All, context.Ownership);
        }

        /// <summary>Existing native proxy settings remain unchanged when AddProxy is not used.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExistingProxySettingsAreUnchangedWithoutAddProxy(bool useHttpClientHandler)
        {
            WebProxy proxy = new("http://localhost:8080");
            using HttpMessageHandler handler = useHttpClientHandler
                ? new HttpClientHandler { Proxy = proxy, UseProxy = true, AllowAutoRedirect = false }
                : new SocketsHttpHandler { Proxy = proxy, UseProxy = true, AllowAutoRedirect = false };
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();

            Assert.AreSame(proxy, handler is HttpClientHandler native ? native.Proxy : ((SocketsHttpHandler)handler).Proxy);
            AssertProxy(handler, "http://localhost:8080/", false);
        }

        /// <summary>The default builder keeps its historical redirect policy and native proxy defaults.</summary>
        [TestMethod]
        public void DefaultBuilderIsUnchangedWithoutAddProxy()
        {
            using HttpClientContext context = new HttpClientContextBuilder().Build();
            SocketsHttpHandler handler = (SocketsHttpHandler)context.HttpMessageHandler;

            Assert.IsNull(handler.Proxy);
            Assert.IsTrue(handler.UseProxy);
            Assert.IsFalse(handler.AllowAutoRedirect);
        }

        /// <summary>Proxy configuration preserves native cookies and ownership in either call order.</summary>
        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, false)]
        [DataRow(false, true, true)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(true, true, false)]
        [DataRow(true, true, true)]
        public void SuppliedHandlerPreservesCookiesAndOwnership(bool useHttpClientHandler, bool proxyFirst, bool owned)
        {
            CookieContainer cookies = new();
            HttpMessageHandlerOwnership ownership = owned ? HttpMessageHandlerOwnership.Owned : HttpMessageHandlerOwnership.Borrowed;
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, cookies);
            HttpClientContextBuilder builder = new();
            if (proxyFirst)
                builder.AddProxy("http://localhost:8080", false).AddHandler(handler, ownership);
            else
                builder.AddHandler(handler, ownership).AddProxy("http://localhost:8080", false);
            using HttpClientContext context = builder.Build();

            Assert.AreSame(handler, context.HttpMessageHandler);
            Assert.AreSame(cookies, context.CookieContainer);
            Assert.IsFalse(handler is HttpClientHandler native ? native.UseCookies : ((SocketsHttpHandler)handler).UseCookies);
            Assert.AreEqual(owned ? HttpClientContextOwnership.All : HttpClientContextOwnership.HttpClient, context.Ownership);
            AssertProxy(handler, "http://localhost:8080/", false);
        }

        /// <summary>Proxy registration and explicit cookie binding coexist in either order.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExplicitCookieContainerIsRetained(bool proxyFirst)
        {
            CookieContainer cookies = new();
            HttpClientContextBuilder builder = new();
            if (proxyFirst)
                builder.AddProxy("http://localhost:8080", false).AddCookieContainer(cookies);
            else
                builder.AddCookieContainer(cookies).AddProxy("http://localhost:8080", false);
            using HttpClientContext context = builder.Build();

            Assert.AreSame(cookies, context.CookieContainer);
            Assert.AreSame(cookies, ((SocketsHttpHandler)context.HttpMessageHandler).CookieContainer);
            AssertProxy(context.HttpMessageHandler, "http://localhost:8080/", false);
        }

        /// <summary>All supported native proxy schemes can be configured without contacting a server.</summary>
        [TestMethod]
        [DataRow("http")]
        [DataRow("https")]
        [DataRow("socks4")]
        [DataRow("socks4a")]
        [DataRow("socks5")]
        public void NativeProxySchemesAreAccepted(string scheme)
        {
            string address = $"{scheme}://localhost:8080/";
            using HttpClientContext context = new HttpClientContextBuilder().AddProxy(address, false).Build();
            AssertProxy(context.HttpMessageHandler, address, false);
        }

        /// <summary>Invalid or ambiguous addresses fail before mutating an existing handler.</summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("localhost:8080")]
        [DataRow("/relative")]
        [DataRow("http://")]
        [DataRow("ftp://localhost:8080")]
        [DataRow("http://user:password@localhost:8080")]
        [DataRow("http://localhost:8080/path")]
        [DataRow("http://localhost:8080?query")]
        [DataRow("http://localhost:8080#fragment")]
        public void InvalidProxyUriDoesNotChangeExistingSettings(string? address)
        {
            HttpClientContextBuilder builder = new();
            using HttpClientContext context = builder.AddProxy("http://localhost:8080", false).Build();
            if (address == null)
                Assert.ThrowsException<ArgumentNullException>(() => builder.AddProxy(address!, true));
            else
                Assert.ThrowsException<ArgumentException>(() => builder.AddProxy(address, true));
            AssertProxy(context.HttpMessageHandler, "http://localhost:8080/", false);
        }

        /// <summary>New and existing configured handlers receive the proxy without losing other settings.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ConfigureHandlerWorksInEitherOrder(bool proxyFirst)
        {
            HttpClientContextBuilder builder = new();
            if (proxyFirst)
                builder.AddProxy("http://localhost:8080", false);
            builder.ConfigureHandler(handler =>
            {
                if (proxyFirst)
                    AssertProxy(handler, "http://localhost:8080/", false);
                ((SocketsHttpHandler)handler).PooledConnectionLifetime = TimeSpan.FromMinutes(2);
            });
            if (!proxyFirst)
                builder.AddProxy("http://localhost:8080", false);
            using HttpClientContext context = builder.Build();

            AssertProxy(context.HttpMessageHandler, "http://localhost:8080/", false);
            Assert.AreEqual(TimeSpan.FromMinutes(2), ((SocketsHttpHandler)context.HttpMessageHandler).PooledConnectionLifetime);
        }

        /// <summary>Proxy credentials configured through the callback are not lost during Build.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ConfigureHandlerCanSetProxyCredentials(bool useHttpClientHandler)
        {
            NetworkCredential credentials = new("proxy-user", "test-password");
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, new CookieContainer());
            HttpClientContextBuilder builder = new();
            builder.AddHandler(handler).AddProxy("http://localhost:8080", false).ConfigureHandler(selected =>
            {
                IWebProxy proxy = (selected is HttpClientHandler native ? native.Proxy : ((SocketsHttpHandler)selected).Proxy)!;
                proxy.Credentials = credentials;
            });
            using HttpClientContext context = builder.Build();
            IWebProxy configured = (handler is HttpClientHandler native ? native.Proxy : ((SocketsHttpHandler)handler).Proxy)!;

            Assert.AreSame(credentials, configured.Credentials);
        }

        /// <summary>Build retains deliberate changes made by the callback after AddProxy.</summary>
        [TestMethod]
        public void LaterConfigurationCanOverrideProxySettings()
        {
            HttpClientContextBuilder builder = new();
            builder.AddProxy("http://localhost:8080", false).ConfigureHandler(handler =>
            {
                SocketsHttpHandler native = (SocketsHttpHandler)handler;
                native.UseProxy = false;
                native.AllowAutoRedirect = true;
            });
            using HttpClientContext context = builder.Build();

            Assert.IsFalse(((SocketsHttpHandler)context.HttpMessageHandler).UseProxy);
            Assert.IsTrue(((SocketsHttpHandler)context.HttpMessageHandler).AllowAutoRedirect);
        }

        /// <summary>An opaque handler cannot silently bypass an explicitly requested proxy.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UnsupportedHandlerIsRejectedWithoutReplacingState(bool proxyFirst)
        {
            using RecordingMessageHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            HttpClientContextBuilder builder = new();
            if (proxyFirst)
            {
                builder.AddProxy("http://localhost:8080", false);
                Assert.ThrowsException<NotSupportedException>(() => builder.AddHandler(handler));
                using HttpClientContext context = builder.Build();
                AssertProxy(context.HttpMessageHandler, "http://localhost:8080/", false);
            }
            else
            {
                builder.AddHandler(handler);
                Assert.ThrowsException<NotSupportedException>(() => builder.AddProxy("http://localhost:8080", false));
                using HttpClientContext context = builder.Build();
                Assert.AreSame(handler, context.HttpMessageHandler);
            }
        }

        /// <summary>The latest proxy selection applies to a replacement native handler.</summary>
        [TestMethod]
        public void ReplacementHandlerUsesLatestProxy()
        {
            using SocketsHttpHandler first = new();
            using HttpClientHandler second = new();
            HttpClientContextBuilder builder = new();
            builder.AddHandler(first).AddProxy("http://localhost:8080", false).AddProxy("http://localhost:8081", true);
            using HttpClientContext context = builder.AddHandler(second).Build();

            AssertProxy(first, "http://localhost:8081/", true);
            AssertProxy(second, "http://localhost:8081/", true);
        }

        /// <summary>Real proxy requests obey redirects and retain cookies across calls and context recreation.</summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task ProxyTransportPreservesRedirectsCookiesAndRebuilds(bool useHttpClientHandler, bool allowAutoRedirect)
        {
            string start = "http://127.0.0.1:1/start";
            string next = "http://127.0.0.1:1/next";
            string redirect = LoopbackCookieServer.Response(302, $"Location: {next}", "Set-Cookie: session=retained; Path=/");
            string[] responses = allowAutoRedirect
                ? [redirect, LoopbackCookieServer.Response(200), LoopbackCookieServer.Response(200)]
                : [redirect, LoopbackCookieServer.Response(200)];
            CookieContainer cookies = new();
            await using LoopbackCookieServer proxy = new(responses);
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, cookies);
            HttpClientContextBuilder builder = new();
            builder.AddHandler(handler).AddCookieContainer(cookies).AddProxy(proxy.BaseUri.AbsoluteUri, allowAutoRedirect).SetTimeout(TimeSpan.FromSeconds(10));
            using (HttpClientContext firstContext = builder.Build())
            using (RestlingClient firstClient = new(firstContext))
            {
                RestRequestResult initial = await firstClient.GetAsync(start);
                Assert.IsNull(initial.Exception, initial.Exception?.ToString());
                Assert.AreEqual(allowAutoRedirect ? HttpStatusCode.OK : HttpStatusCode.Found, initial.StatusCode);
                Assert.ThrowsException<InvalidOperationException>(() => builder.AddProxy("http://localhost:8080", true));
            }

            using HttpClientContext nextContext = builder.Build();
            using RestlingClient nextClient = new(nextContext);
            RestRequestResult result = await nextClient.GetAsync(next);
            Assert.IsTrue(result.IsSuccessful, result.Exception?.ToString());
            IReadOnlyList<LoopbackCookieServer.Request> requests = await proxy.Requests;

            Assert.AreEqual(allowAutoRedirect ? 3 : 2, requests.Count);
            Assert.AreEqual(start, requests[0].Target);
            foreach (LoopbackCookieServer.Request request in requests.Skip(1))
            {
                Assert.AreEqual(next, request.Target);
                StringAssert.Contains(request.Headers["Cookie"], "session=retained");
            }
            Assert.AreSame(cookies, nextContext.CookieContainer);
        }

        /// <summary>Creates a borrowed native handler with disabled policies to verify explicit changes.</summary>
        private static HttpMessageHandler CreateHandler(bool useHttpClientHandler, CookieContainer cookies)
        {
            return useHttpClientHandler
                ? new HttpClientHandler { CookieContainer = cookies, UseCookies = false, UseProxy = false }
                : new SocketsHttpHandler { CookieContainer = cookies, UseCookies = false, UseProxy = false };
        }

        /// <summary>Checks the effective native proxy and redirect policy.</summary>
        private static void AssertProxy(HttpMessageHandler handler, string address, bool allowAutoRedirect)
        {
            IWebProxy? proxy = handler is HttpClientHandler native ? native.Proxy : ((SocketsHttpHandler)handler).Proxy;
            Assert.IsInstanceOfType<WebProxy>(proxy);
            Assert.AreEqual(new Uri(address), ((WebProxy)proxy).Address);
            Assert.IsTrue(handler is HttpClientHandler httpHandler ? httpHandler.UseProxy : ((SocketsHttpHandler)handler).UseProxy);
            Assert.AreEqual(allowAutoRedirect, handler is HttpClientHandler clientHandler ? clientHandler.AllowAutoRedirect : ((SocketsHttpHandler)handler).AllowAutoRedirect);
        }

        #endregion
    }
}
