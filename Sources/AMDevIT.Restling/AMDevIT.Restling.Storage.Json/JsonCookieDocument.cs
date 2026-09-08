namespace AMDevIT.Restling.Storage.Json
{
    internal sealed class JsonCookieDocument
    {
        #region Properties

        public SortedDictionary<string, JsonCookieRecord> Cookies { get; set; } = [];
        public string Format { get; set; } = "Restling.Storage.Json.Plain";
        public int Version { get; set; } = 1;

        #endregion
    }
}
