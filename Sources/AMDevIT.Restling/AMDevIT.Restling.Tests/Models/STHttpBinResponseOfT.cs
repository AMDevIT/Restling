using System.Text.Json.Serialization;

namespace AMDevIT.Restling.Tests.Models
{
    public class STHttpBinResponse<T>
    {
        #region Properties

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("json")]
        public T? Json { get; set; }

        #endregion
    }
}
