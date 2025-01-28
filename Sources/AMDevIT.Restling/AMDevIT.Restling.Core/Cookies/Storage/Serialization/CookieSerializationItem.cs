using Newtonsoft.Json;

namespace AMDevIT.Restling.Core.Cookies.Storage.Serialization
{
    [JsonObject]
    public class CookieSerializationItem
    {
        #region Properties

        [JsonProperty("domain")]
        public string? Domain
        {
            get;
            set;
        }

        [JsonProperty("path")]
        public string? Path
        {
            get;
            set;
        }

        [JsonProperty("uri")]
        public string? Uri
        {
            get;
            set;
        }

        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        } = null!;

        [JsonProperty("value")]
        public string? Value
        {
            get;
            set;
        }

        #endregion

        #region .ctor

        public CookieSerializationItem()
        {

        }

        public CookieSerializationItem(string? domain, 
                                    string? path, 
                                    string? uri, 
                                    string name, 
                                    string? value)
        {
            this.Domain = domain;
            this.Path = path;
            this.Uri = uri;
            this.Name = name;
            this.Value = value;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"[Domain:{this.Domain},Path:{this.Path},Uri:{this.Uri}]{this.Name}={this.Value}";
        }

        #endregion
    }
}
