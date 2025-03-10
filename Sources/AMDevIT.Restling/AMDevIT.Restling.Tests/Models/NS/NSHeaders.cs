
using Newtonsoft.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AMDevIT.Restling.Tests.Models.NS
{
    public class NSHeaders
    {
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonInclude]
        [System.Text.Json.Serialization.JsonPropertyName("Host")]
        public string? Host { get; private set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("User - Agent")]
        [System.Text.Json.Serialization.JsonInclude]
        public string? UserAgent { get; private set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("X-Amzn-Trace-Id")]
        [System.Text.Json.Serialization.JsonInclude]
        public string? TraceId { get; private set; }
    }
}
