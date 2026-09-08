using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>RFC 9457 fields and extensions. URI references are never fetched automatically.</summary>
    public sealed class RestProblemDetails
    {
        #region Properties

        [JsonPropertyName("type")]
        public string Type { get; set; } = "about:blank";

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("instance")]
        public string? Instance { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> Extensions { get; set; } = new(StringComparer.Ordinal);

        #endregion
    }
}
