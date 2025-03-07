using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models.ST
{
    public class STHeaders
    {
        [JsonPropertyName("Host")]
        [JsonInclude]
        public string? Host { get; private set; }

        [JsonPropertyName("User-Agent")]
        [JsonInclude]
        public string? UserAgent { get; private set; }

        [JsonPropertyName("X-Amzn-Trace-Id")]
        [JsonInclude]
        public string? TraceId { get; private set; }
    }
}
