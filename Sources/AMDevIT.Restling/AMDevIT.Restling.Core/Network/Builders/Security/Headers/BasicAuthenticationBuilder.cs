using AMDevIT.Restling.Core.Security;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace AMDevIT.Restling.Core.Network.Builders.Security.Headers
{
    public class BasicAuthenticationBuilder
        : IAuthenticationBuilder
    {
        #region Consts

        private const string HeaderScheme = "Basic";

        #endregion

        #region Fields

        private string user = null!;
        private SecureString? password;
        private string? hash;

        #endregion

        #region Properties

        public string User => user;

        public string? Password => this.hash ??= ComputePasswordHash(password);

        #endregion

        #region Methods
        
        public AuthenticationHeader Build()
        {
            if (string.IsNullOrWhiteSpace(this.user))
                throw new InvalidOperationException("User is required");

            if (this.password == null)
                throw new InvalidOperationException("Password is required");

            string? headerParamsBase64 = BuildHeaderParams(this.user, this.password);
            AuthenticationHeader authenticationHeader;

            if (string.IsNullOrWhiteSpace(headerParamsBase64))
                throw new InvalidOperationException("Header params are required");

            authenticationHeader = new(HeaderScheme, headerParamsBase64);
            return authenticationHeader;
        }

        public IAuthenticationBuilder SetPassword(string password)
        {
            SecureString secureString = new();

            foreach (char c in password)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            this.password = secureString;
            this.hash = ComputePasswordHash(secureString);
            return this;
        }

        public IAuthenticationBuilder SetPassword(SecureString password)
        {
            this.password = password;
            this.hash = ComputePasswordHash(password);
            return this;
        }

        public IAuthenticationBuilder SetPassword(byte[] password)
        {
            if (password == null || password.Length == 0)
            {
                this.password = null;
                this.hash = null;
                return this;
            }
            
            SecureString secureString = new ();
            char[] chars = Encoding.UTF8.GetChars(password);
            this.hash = ComputePasswordHash(password);
            Array.Clear(password, 0, password.Length);

            try
            {
                foreach (char c in chars)
                {
                    secureString.AppendChar(c);
                }

                secureString.MakeReadOnly();
            }
            finally
            {
                Array.Clear(chars, 0, chars.Length);
            }

            this.password = secureString;
            return this;
        }

        public IAuthenticationBuilder SetUser(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");

            this.user = user;
            return this;
        }

        protected static string? ComputePasswordHash(SecureString? password)
        {
            if (password == null || password.Length == 0)
                return null;

            byte[] passwordBytes = password.ToByteArray();
            string? hash = ComputePasswordHash(passwordBytes);
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            return hash; 
        }

        protected static string? ComputePasswordHash(byte[]? password)
        {
            if (password == null || password.Length == 0)
                return null;

            string? hash = null;
            byte[]? hashBytes = [];

            try
            {
                hashBytes = SHA256.HashData(password);
                hash = Convert.ToHexString(hashBytes).Replace("-", string.Empty);
            }
            finally
            {
                Array.Clear(hashBytes, 0, hashBytes.Length);
            }
            
            return hash;
        }

        protected static string ? BuildHeaderParams(string user, SecureString password)
        { 
            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("User is required");

            if (password == null)
                throw new InvalidOperationException("Password is required");

            string headerParamsBase64;
            byte[]? passwordBytes = password.Length > 0 ? password.ToUtf8ByteArray() : [];
            byte[] userBytes = Encoding.UTF8.GetBytes(user);
            byte[] headerParamsBytes = new byte[userBytes.Length + 1 + passwordBytes.Length];

            try
            {
                Buffer.BlockCopy(userBytes, 0, headerParamsBytes, 0, userBytes.Length);
                headerParamsBytes[userBytes.Length] = (byte)':';
                Buffer.BlockCopy(passwordBytes, 0, headerParamsBytes, userBytes.Length + 1, passwordBytes.Length);

                headerParamsBase64 = Convert.ToBase64String(headerParamsBytes);
            }
            finally
            {
                Array.Clear(headerParamsBytes, 0, headerParamsBytes.Length);
                Array.Clear(userBytes, 0, userBytes.Length);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
            return headerParamsBase64;
        }
    }

    #endregion
}
