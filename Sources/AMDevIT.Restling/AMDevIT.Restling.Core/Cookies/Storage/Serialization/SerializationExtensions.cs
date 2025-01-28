namespace AMDevIT.Restling.Core.Cookies.Storage.Serialization
{
    public static class SerializationExtensions
    {
        #region Methods

        public static HttpCookieData ToCookieData(this CookieSerializationItem source)
        {
            return new HttpCookieData(source.Name, source.Value, source.Domain, source.Path, source.Uri);
        }

        public static CookieSerializationItem ToSerializationItem(this HttpCookieData source)
        {
            return new CookieSerializationItem(source.Domain, source.Path, source.Uri, source.Name, source.Value);
        }

        #endregion
    }
}
