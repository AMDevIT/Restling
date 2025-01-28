namespace AMDevIT.Restling.Tests.Models
{
    public class HttpBinResponse
    {
        #region Properties

        public Dictionary<string, string>? Args { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Origin { get; set; } 
        public string? Url { get; set; } 
        public Dictionary<string, string>? Form { get; set; }
        public Dictionary<string, string>? Files { get; set; }

        #endregion
    }
}
