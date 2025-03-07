
using Newtonsoft.Json;

namespace AMDevIT.Restling.Tests.Models.NS
{
    public class NSArguments
    {
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonPropertyName("test")]
        [System.Text.Json.Serialization.JsonInclude]
        public string? Test { get; set; }
    }
}
