using System.Net.Http.Headers;
using System.Security;

namespace AMDevIT.Restling.Core.Network.Builders.Security.Headers
{
    public interface IAuthenticationBuilder
    {
        #region Methods

        IAuthenticationBuilder SetUser(string user);
        IAuthenticationBuilder SetPassword(SecureString secureString);
        IAuthenticationBuilder SetPassword(byte[] password);
        IAuthenticationBuilder SetPassword(string password);
        AuthenticationHeader Build();

        #endregion
    }
}
