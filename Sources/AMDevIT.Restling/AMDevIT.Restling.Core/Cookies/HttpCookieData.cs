namespace AMDevIT.Restling.Core.Cookies
{
    public class HttpCookieData(string name,
                                string? value,
                                string? domain = null, 
                                string? path = null, 
                                string? uri = null)
    {
        #region Fields

        private readonly string? domain = domain;
        private readonly string? path = path;
        private readonly string? uri = uri;
        private readonly string name = name;

        private string? value = value;

        #endregion

        #region Properties

        public string? Domain => this.domain;
        public string? Path => this.path;
        public string? Uri => this.uri;
        public string Name => this.name;
        public string? Value
        {
            get => this.value;
            set => this.value = value;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"[Domain:{this.Domain},Path:{this.path},Uri:{this.Uri}]{this.Name}={this.Value}";
        }

        #endregion
    }
}
