using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Tests.Ownership;
using System.Net;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class OwnershipTests
    {
        #region Methods

        /// <summary>Verifies that the legacy context constructor retains ownership of both resources.</summary>
        [TestMethod]
        public void LegacyContextConstructorOwnsAllResources()
        {
            TrackingMessageHandler handler = new();
            HttpClient httpClient = new(handler, disposeHandler: false);
            HttpClientContext context = new(httpClient, handler, new CookieContainer());

            context.Dispose();

            Assert.AreEqual(HttpClientContextOwnership.All, context.Ownership);
            Assert.IsTrue(context.Disposed);
            Assert.IsTrue(handler.Disposed);
        }

        /// <summary>Verifies that explicit HttpClient ownership does not dispose a borrowed handler.</summary>
        [TestMethod]
        public void ContextCanBorrowHandler()
        {
            TrackingMessageHandler handler = new();
            HttpClient httpClient = new(handler, disposeHandler: false);
            HttpClientContext context = new(httpClient,
                                            handler,
                                            new CookieContainer(),
                                            HttpClientContextOwnership.HttpClient);

            context.Dispose();

            Assert.IsTrue(context.Disposed);
            Assert.IsFalse(handler.Disposed);
            handler.Dispose();
        }

        /// <summary>Verifies builder ownership for borrowed and owned handlers.</summary>
        [TestMethod]
        public void BuilderPreservesHandlerOwnershipChoice()
        {
            TrackingMessageHandler borrowedHandler = new();
            TrackingMessageHandler ownedHandler = new();
            HttpClientContext borrowedContext = new HttpClientContextBuilder().AddHandler(borrowedHandler).Build();
            HttpClientContext ownedContext = new HttpClientContextBuilder().AddHandler(ownedHandler,
                                                                                      HttpMessageHandlerOwnership.Owned)
                                                                              .Build();

            borrowedContext.Dispose();
            ownedContext.Dispose();

            Assert.AreEqual(HttpClientContextOwnership.HttpClient, borrowedContext.Ownership);
            Assert.AreEqual(HttpClientContextOwnership.All, ownedContext.Ownership);
            Assert.IsFalse(borrowedHandler.Disposed);
            Assert.IsTrue(ownedHandler.Disposed);
            borrowedHandler.Dispose();
        }

        /// <summary>Verifies that the legacy boolean overload maps to owned handler semantics.</summary>
        [TestMethod]
        public void BuilderPreservesLegacyHandlerOwnershipChoice()
        {
            TrackingMessageHandler handler = new();
            HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler, diposeHandler: true).Build();

            context.Dispose();

            Assert.AreEqual(HttpClientContextOwnership.All, context.Ownership);
            Assert.IsTrue(handler.Disposed);
        }

        /// <summary>Verifies that a client borrows an externally supplied context by default.</summary>
        [TestMethod]
        public void ClientBorrowsExternalContext()
        {
            TrackingMessageHandler handler = new();
            HttpClient httpClient = new(handler, disposeHandler: false);
            HttpClientContext context = new(httpClient,
                                            handler,
                                            new CookieContainer(),
                                            HttpClientContextOwnership.All);
            RestlingClient client = new(context);

            client.Dispose();

            Assert.AreEqual(RestlingClientContextOwnership.Borrowed, client.ContextOwnership);
            Assert.IsFalse(context.Disposed);
            context.Dispose();
        }

        /// <summary>Verifies explicit ownership and the backward-compatible boolean alias.</summary>
        [TestMethod]
        public void ClientCanOwnExternalContextExplicitly()
        {
            TrackingMessageHandler handler = new();
            HttpClient httpClient = new(handler, disposeHandler: false);
            HttpClientContext context = new(httpClient,
                                            handler,
                                            new CookieContainer(),
                                            HttpClientContextOwnership.All);
            RestlingClient client = new(context, RestlingClientContextOwnership.Borrowed)
            {
                DisposeContext = true
            };

            client.Dispose();

            Assert.AreEqual(RestlingClientContextOwnership.Owned, client.ContextOwnership);
            Assert.IsTrue(context.Disposed);
            Assert.IsTrue(handler.Disposed);
        }

        /// <summary>Verifies that constructors creating a context also own it.</summary>
        [TestMethod]
        public void ClientOwnsBuilderContext()
        {
            TrackingMessageHandler handler = new();
            RestlingClient client = new(new HttpClientContextBuilder().AddHandler(handler));
            HttpClientContext context = client.ClientContext;

            client.Dispose();

            Assert.AreEqual(RestlingClientContextOwnership.Owned, client.ContextOwnership);
            Assert.IsTrue(context.Disposed);
            Assert.IsFalse(handler.Disposed);
            handler.Dispose();
        }

        #endregion
    }
}
