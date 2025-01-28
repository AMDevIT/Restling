using AMDevIT.Restling.Tests.Common;
using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models
{
    public class STHttpBinResponse
    {
        #region Properties

        [JsonPropertyName("args")]
        public Dictionary<string, string>? Args { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("form")]
        public Dictionary<string, string>? Form { get; set; }

        [JsonPropertyName("files")]
        public Dictionary<string, string>? Files { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $@"
Args: {StringUtils.DictionaryToString(Args)}
Headers: {StringUtils.DictionaryToString(Headers)}
Origin: {Origin ?? "None"}
Url: {Url ?? "None"}
Form: {StringUtils.DictionaryToString(Form)}
Files: {StringUtils.DictionaryToString(Files)}
";
        }

        #endregion
    }
}
