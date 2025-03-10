using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models.ST
{
    public class STOnlyHttpBinResponse
    {
        [JsonPropertyName("args")]
        [JsonInclude]
        public STArguments? Args { get; private set; } 

        [JsonPropertyName("headers")]
        [JsonInclude]
        public STHeaders? Headers { get; private set; }

        [JsonPropertyName("origin")]
        [JsonInclude]
        public string? Origin { get; private set; }

        [JsonPropertyName("url")]
        [JsonInclude]
        public string? Url { get; private set; }
    }
}
