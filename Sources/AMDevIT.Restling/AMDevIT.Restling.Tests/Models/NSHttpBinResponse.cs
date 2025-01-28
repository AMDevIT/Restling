using AMDevIT.Restling.Tests.Common;
using Newtonsoft.Json;

namespace AMDevIT.Restling.Tests.Models
{
    public class NSHttpBinResponse
    {

        #region Properties

        [JsonProperty("args")]
        public Dictionary<string, string>? Args { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonProperty("origin")]
        public string? Origin { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("form")]
        public Dictionary<string, string>? Form { get; set; }

        [JsonProperty("files")]
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
