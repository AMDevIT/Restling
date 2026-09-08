using System.Net;
using System.Text;

namespace AMDevIT.Restling.Storage.Json
{
    internal sealed class JsonCookieRecord
    {
        #region Properties

        public string Comment { get; set; } = string.Empty;
        public string? CommentUri { get; set; }
        public bool Discard { get; set; }
        public string Domain { get; set; } = null!;
        public DateTime Expires { get; set; }
        public bool HttpOnly { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = "/";
        public string Port { get; set; } = string.Empty;
        public bool Secure { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Version { get; set; }

        #endregion

        #region Methods

        /// <summary>Captures the public state of a native cookie.</summary>
        public static JsonCookieRecord FromCookie(Cookie cookie)
        {
            return new JsonCookieRecord
            {
                Comment = cookie.Comment,
                CommentUri = cookie.CommentUri?.AbsoluteUri,
                Discard = cookie.Discard,
                Domain = cookie.Domain,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly,
                Name = cookie.Name,
                Path = cookie.Path,
                Port = cookie.Port,
                Secure = cookie.Secure,
                Value = cookie.Value,
                Version = cookie.Version
            };
        }

        /// <summary>Builds the stable dictionary key for this cookie identity.</summary>
        public string GetKey()
        {
            string identity = $"{this.Domain.ToLowerInvariant()}\n{this.Path}\n{this.Name.ToLowerInvariant()}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(identity))
                          .TrimEnd('=')
                          .Replace('+', '-')
                          .Replace('/', '_');
        }

        /// <summary>Recreates a native cookie from the persisted state.</summary>
        public Cookie ToCookie()
        {
            Cookie cookie = new(this.Name, this.Value, this.Path, this.Domain)
            {
                Comment = this.Comment,
                Discard = this.Discard,
                Expires = this.Expires,
                HttpOnly = this.HttpOnly,
                Secure = this.Secure,
                Version = this.Version
            };

            if (!string.IsNullOrWhiteSpace(this.CommentUri))
                cookie.CommentUri = new Uri(this.CommentUri);
            if (!string.IsNullOrWhiteSpace(this.Port))
                cookie.Port = this.Port;
            return cookie;
        }

        #endregion
    }
}
