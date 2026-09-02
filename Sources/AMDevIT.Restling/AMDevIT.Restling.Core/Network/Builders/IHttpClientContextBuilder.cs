using AMDevIT.Restling.Core.Cookies;
using System.Collections.ObjectModel;
using AMDevIT.Restling.Core.Codecs;
using System.Net;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Network.Builders
{
    public interface IHttpClientContextBuilder
    {
        #region Properties

        ReadOnlyCollection<HttpCookieData> Cookies { get; }

        #endregion

        #region Methods

        /// <summary>Adds a codec when supported by the builder. Existing custom builders need not implement this member.</summary>
        HttpClientContextBuilder AddCodec(IContentCodec codec)
        {
            throw new NotSupportedException("This builder does not support codec registration. Configure HttpClientContext.Codecs instead.");
        }

        #region Cookies

        HttpClientContextBuilder AddCookie(HttpCookieData cookie);
        HttpClientContextBuilder AddCookieContainer(CookieContainer cookieContainer);
        HttpClientContextBuilder AddCookies(IEnumerable<HttpCookieData> cookies);
        HttpClientContextBuilder ClearCookieContainer();
        HttpClientContextBuilder ClearCookies();
        HttpClientContextBuilder RemoveCookie(HttpCookieData cookie);
        HttpClientContextBuilder RemoveCookies(IEnumerable<HttpCookieData> cookies);

        #endregion

        #region Headers

        HttpClientContextBuilder AddDefaultHeader(string name, string value);
        HttpClientContextBuilder RemoveDefaultHeader(string name);
        HttpClientContextBuilder ClearDefaultHeaders();
        HttpClientContextBuilder AddUserAgent(string? userAgent);
        HttpClientContextBuilder AddAuthenticationHeader(string scheme, string parameter);
        HttpClientContextBuilder AddAuthenticationHeader(AuthenticationHeaderValue authenticationHeaderValue);
        HttpClientContextBuilder AddAuthenticationHeader(AuthenticationHeader authenticationHeader);
        HttpClientContextBuilder RemoveAuthenticationHeader();

        #endregion

        #region Handlers

        HttpClientContextBuilder AddHandler(HttpMessageHandler handler, bool diposeHandler = false);

        /// <summary>Adds a handler with an explicit ownership contract.</summary>
        /// <param name="handler">The message handler used by the generated HTTP client.</param>
        /// <param name="ownership">Whether the generated context borrows or owns the handler.</param>
        /// <returns>The current builder instance.</returns>
        HttpClientContextBuilder AddHandler(HttpMessageHandler handler, HttpMessageHandlerOwnership ownership)
        {
            if (!Enum.IsDefined(ownership))
                throw new ArgumentOutOfRangeException(nameof(ownership));
            return this.AddHandler(handler, ownership == HttpMessageHandlerOwnership.Owned);
        }
        HttpClientContextBuilder ConfigureHandler(Action<HttpMessageHandler> configureHandler);

        /// <summary>Selects an explicit proxy and HTTP redirect policy when supported by the builder.</summary>
        /// <param name="proxyUri">An absolute proxy URI without embedded credentials.</param>
        /// <param name="allowAutoRedirect">Whether the handler automatically follows HTTP response redirects.</param>
        /// <returns>The current builder instance.</returns>
        /// <remarks>Existing custom builders need not implement this member.</remarks>
        HttpClientContextBuilder AddProxy(string proxyUri, bool allowAutoRedirect)
        {
            throw new NotSupportedException("This builder does not support proxy configuration. Configure its transport handler explicitly.");
        }

        #endregion

        #region Http parameters

        HttpClientContextBuilder SetTimeout(TimeSpan? timeout);

        #endregion

        HttpClientContext Build();

        #endregion
    }
}
