using Newtonsoft.Json;

namespace AMDevIT.Restling.Tests.Models.NS
{
    public class NSOnlyHttpBinResponse
    {
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("args")]
        [System.Text.Json.Serialization.JsonInclude]
        public NSArguments? Args { get; private set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("headers")]
        [System.Text.Json.Serialization.JsonInclude]
        public NSHeaders? Headers { get; private set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("origin")]
        [System.Text.Json.Serialization.JsonInclude]
        public string? Origin { get; private set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        [System.Text.Json.Serialization.JsonInclude]
        public string? Url { get; private set; }
    }
}
