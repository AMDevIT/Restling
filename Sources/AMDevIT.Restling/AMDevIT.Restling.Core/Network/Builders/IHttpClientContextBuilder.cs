using AMDevIT.Restling.Core.Cookies;
using System.Collections.ObjectModel;
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
        HttpClientContextBuilder ConfigureHandler(Action<HttpMessageHandler> configureHandler);

        #endregion

        #region Http parameters

        HttpClientContextBuilder SetTimeout(TimeSpan? timeout);

        #endregion

        HttpClientContext Build();

        #endregion
    }
}