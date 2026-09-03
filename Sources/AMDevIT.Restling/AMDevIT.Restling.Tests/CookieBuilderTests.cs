using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Cookies;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Tests.Cookies;
using AMDevIT.Restling.Tests.Multipart;
using System.Net;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class CookieBuilderTests
    {
        #region Methods

        /// <summary>Adopts native cookies and preserves an explicitly disabled native cookie policy.</summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void NativeContainerAndSettingsArePreserved(bool useHttpClientHandler, bool useCookies)
        {
            Uri uri = new("http://example.test/");
            CookieContainer cookies = new();
            cookies.Add(uri, new Cookie("existing", "kept", "/"));
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, cookies, useCookies);
            HttpClientContextBuilder builder = new();
            builder.AddHandler(handler).AddCookie(new HttpCookieData("added", "value", "example.test", "/"));
            using HttpClientContext context = builder.Build();

            Assert.AreSame(cookies, context.CookieContainer);
            Assert.AreSame(cookies, NativeContainer(handler));
            Assert.AreEqual("kept", context.CookieContainer.GetCookies(uri)["existing"]?.Value);
            Assert.AreEqual("value", cookies.GetCookies(uri)["added"]?.Value);
            Assert.AreEqual(useCookies, NativeUseCookies(handler));
            Assert.IsFalse(handler is HttpClientHandler httpHandler ? httpHandler.AllowAutoRedirect : ((SocketsHttpHandler)handler).AllowAutoRedirect);
            Assert.AreEqual(HttpClientContextOwnership.HttpClient, context.Ownership);
        }

        /// <summary>Explicit cookie containers enable native cookie handling regardless of builder call order.</summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void ExplicitContainerEnablesCookiesInEitherOrder(bool useHttpClientHandler, bool containerFirst)
        {
            CookieContainer cookies = new();
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, new CookieContainer(), false);
            HttpClientContextBuilder builder = new();
            if (containerFirst)
                builder.AddCookieContainer(cookies).AddHandler(handler);
            else
                builder.AddHandler(handler).AddCookieContainer(cookies);
            using HttpClientContext context = builder.Build();

            Assert.AreSame(cookies, NativeContainer(handler));
            Assert.AreSame(cookies, context.CookieContainer);
            Assert.IsTrue(NativeUseCookies(handler));
        }

        /// <summary>Newly configured handlers share an explicit container before and after the callback.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ConfigureHandlerSharesExplicitContainer(bool containerFirst)
        {
            CookieContainer cookies = new();
            HttpClientContextBuilder builder = new();
            if (containerFirst)
                builder.AddCookieContainer(cookies);
            builder.ConfigureHandler(handler =>
            {
                SocketsHttpHandler native = (SocketsHttpHandler)handler;
                if (containerFirst)
                    Assert.AreSame(cookies, native.CookieContainer);
                native.AllowAutoRedirect = false;
            });
            if (!containerFirst)
                builder.AddCookieContainer(cookies);
            using HttpClientContext context = builder.Build();

            Assert.AreSame(cookies, context.CookieContainer);
            Assert.AreSame(cookies, NativeContainer(context.HttpMessageHandler));
            Assert.IsFalse(((SocketsHttpHandler)context.HttpMessageHandler).AllowAutoRedirect);
            Assert.AreEqual(HttpClientContextOwnership.All, context.Ownership);
        }

        /// <summary>Build does not undo a deliberate UseCookies change made after selecting a container.</summary>
        [TestMethod]
        public void ConfigureHandlerCanDisableExplicitCookies()
        {
            CookieContainer cookies = new();
            HttpClientContextBuilder builder = new();
            builder.AddCookieContainer(cookies).ConfigureHandler(handler => ((SocketsHttpHandler)handler).UseCookies = false);
            using HttpClientContext context = builder.Build();

            Assert.AreSame(cookies, context.CookieContainer);
            Assert.IsFalse(NativeUseCookies(context.HttpMessageHandler));
        }

        /// <summary>Replacing a handler without an explicit jar adopts the replacement's own cookies.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReplacingNativeHandlerDoesNotOverwriteItsContainer(bool useHttpClientHandler)
        {
            CookieContainer firstCookies = new();
            CookieContainer secondCookies = new();
            using HttpMessageHandler first = CreateHandler(useHttpClientHandler, firstCookies, true);
            using HttpMessageHandler second = CreateHandler(useHttpClientHandler, secondCookies, true);
            HttpClientContextBuilder builder = new();
            using HttpClientContext firstContext = builder.AddHandler(first).Build();
            using HttpClientContext secondContext = builder.AddHandler(second).Build();

            Assert.AreSame(firstCookies, firstContext.CookieContainer);
            Assert.AreSame(secondCookies, secondContext.CookieContainer);
            Assert.AreSame(secondCookies, NativeContainer(second));
        }

        /// <summary>A fallback jar for a custom handler must not replace a later native handler's cookies.</summary>
        [TestMethod]
        public void CustomHandlerFallbackIsNotAnExplicitContainer()
        {
            CookieContainer nativeCookies = new();
            using RecordingMessageHandler custom = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using SocketsHttpHandler native = new() { CookieContainer = nativeCookies };
            HttpClientContextBuilder builder = new();
            using HttpClientContext customContext = builder.AddHandler(custom).Build();
            using HttpClientContext nativeContext = builder.AddHandler(native).Build();

            Assert.AreSame(nativeCookies, nativeContext.CookieContainer);
            Assert.AreNotSame(customContext.CookieContainer, nativeContext.CookieContainer);
        }

        /// <summary>Clearing the cookie container selects a fresh native jar without erasing the old jar.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ClearCookieContainerSelectsAFreshJar(bool useHttpClientHandler)
        {
            CookieContainer existing = new();
            existing.Add(new Uri("http://example.test/"), new Cookie("session", "kept", "/"));
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, existing, true);
            using HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).ClearCookieContainer().Build();

            Assert.AreNotSame(existing, context.CookieContainer);
            Assert.AreSame(context.CookieContainer, NativeContainer(handler));
            Assert.AreEqual(0, context.CookieContainer.Count);
            Assert.AreEqual(1, existing.Count);
        }

        /// <summary>Repeated builds can reuse an active handler without resetting its container or cookie state.</summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RebuildingWithAnActiveHandlerPreservesCookies(bool useHttpClientHandler)
        {
            CookieContainer cookies = new();
            await using LoopbackCookieServer server = new(LoopbackCookieServer.Response(200, "Set-Cookie: session=retained; Path=/"),
                                                         LoopbackCookieServer.Response(200));
            using HttpMessageHandler handler = CreateHandler(useHttpClientHandler, cookies, true);
            HttpClientContextBuilder builder = new();
            builder.AddCookieContainer(cookies).AddHandler(handler).SetTimeout(TimeSpan.FromSeconds(10));
            using (HttpClientContext firstContext = builder.Build())
            using (RestlingClient firstClient = new(firstContext))
            {
                RestRequestResult initial = await firstClient.GetAsync(server.BaseUri.AbsoluteUri);
                Assert.IsTrue(initial.IsSuccessful, initial.Exception?.ToString());
            }

            using HttpClientContext nextContext = builder.Build();
            using RestlingClient nextClient = new(nextContext);
            RestRequestResult result = await nextClient.GetAsync(server.BaseUri.AbsoluteUri);
            IReadOnlyList<LoopbackCookieServer.Request> requests = await server.Requests;

            Assert.IsTrue(result.IsSuccessful, result.Exception?.ToString());
            Assert.AreSame(cookies, nextContext.CookieContainer);
            StringAssert.Contains(requests[1].Headers["Cookie"], "session=retained");
        }

        /// <summary>Creates a native handler with explicit settings for preservation checks.</summary>
        private static HttpMessageHandler CreateHandler(bool useHttpClientHandler, CookieContainer cookies, bool useCookies)
        {
            return useHttpClientHandler
                ? new HttpClientHandler { CookieContainer = cookies, UseCookies = useCookies, AllowAutoRedirect = false, UseProxy = false }
                : new SocketsHttpHandler { CookieContainer = cookies, UseCookies = useCookies, AllowAutoRedirect = false, UseProxy = false };
        }

        /// <summary>Reads the actual cookie jar used by a native handler.</summary>
        private static CookieContainer NativeContainer(HttpMessageHandler handler)
        {
            return handler is HttpClientHandler httpHandler ? httpHandler.CookieContainer : ((SocketsHttpHandler)handler).CookieContainer;
        }

        /// <summary>Reads the actual native cookie enablement setting.</summary>
        private static bool NativeUseCookies(HttpMessageHandler handler)
        {
            return handler is HttpClientHandler httpHandler ? httpHandler.UseCookies : ((SocketsHttpHandler)handler).UseCookies;
        }

        #endregion
    }
}
