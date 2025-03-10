using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models.ST
{
    public class STArguments
    {
        [JsonPropertyName("test")]
        [JsonInclude]
        public string? Test { get; private set; }
    }
}
