using AMDevIT.Restling.Core.Cookies;
using System.Collections.ObjectModel;
using System.Net;

namespace AMDevIT.Restling.Core.Network.Builders
{
    public interface IHttpClientContextBuilder
    {
        ReadOnlyCollection<HttpCookieData> Cookies { get; }

        HttpClientContextBuilder AddCookie(HttpCookieData cookie);
        HttpClientContextBuilder AddCookieContainer(CookieContainer cookieContainer);
        HttpClientContextBuilder AddCookies(IEnumerable<HttpCookieData> cookies);
        HttpClientContextBuilder AddDefaultHeader(string name, string value);
        HttpClientContextBuilder AddHandler(HttpMessageHandler handler, bool diposeHandler = false);
        HttpClientContextBuilder AddUserAgent(string? userAgent);
        HttpClientContext Build();
        HttpClientContextBuilder ClearCookieContainer();
        HttpClientContextBuilder ClearCookies();
        HttpClientContextBuilder ConfigureHandler(Action<HttpMessageHandler> configureHandler);
        HttpClientContextBuilder RemoveCookie(HttpCookieData cookie);
        HttpClientContextBuilder RemoveCookies(IEnumerable<HttpCookieData> cookies);
        HttpClientContextBuilder RemoveDefaultHeader(string name);
        HttpClientContextBuilder SetTimeout(TimeSpan? timeout);
    }
}