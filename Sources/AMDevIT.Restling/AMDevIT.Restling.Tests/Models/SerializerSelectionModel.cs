using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models
{
    public sealed class SerializerSelectionModel
    {
        #region Properties

        [JsonProperty("newtonsoft_name")]
        [JsonPropertyName("system_name")]
        public string? Name { get; set; }

        #endregion
    }
}
