using Newtonsoft.Json;

namespace AMDevIT.Restling.Tests.Models.Authentication
{
    [JsonObject]
    public class BasicAuthResult
    {
        #region Properties

        [JsonProperty("authenticated")]
        public bool Authenticated 
        { 
            get; 
            set; 
        }
        [JsonProperty("user")]
        public string? User 
        { 
            get; 
            set; 
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"Authenticated: {this.Authenticated}, User: {this.User}";
        }

        #endregion
    }
}
