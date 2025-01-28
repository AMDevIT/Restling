using Newtonsoft.Json;

namespace AMDevIT.Restling.Tests.Models
{
    public class NSHttpBinResponse<T>
        : NSHttpBinResponse
    {
        #region Properties

        [JsonProperty("data")]
        public T? Data { get; set; }

        [JsonProperty("json")]
        public T? Json { get; set; }

        #endregion
    }
}
