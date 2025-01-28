namespace AMDevIT.Restling.Core.Network
{
    public class AuthenticationHeader(string scheme, 
                                      string value)
    {
        #region Fields

        private readonly string scheme = scheme;
        private string value = value;

        #endregion

        #region Properties

        public string Scheme => this.scheme;

        public string Parameter
        {
            get => this.value;
            set => this.value = value;
        }

        #endregion
    }
}
