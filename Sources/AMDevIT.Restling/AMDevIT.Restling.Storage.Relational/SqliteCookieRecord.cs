using System.Net;

namespace AMDevIT.Restling.Storage.Relational
{
    internal sealed class SqliteCookieRecord
    {
        #region Properties

        public string Comment { get; set; } = string.Empty;
        public string? CommentUri { get; set; }
        public bool Discard { get; set; }
        public string Domain { get; set; } = null!;
        public long ExpiresBinary { get; set; }
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
        public static SqliteCookieRecord FromCookie(Cookie cookie)
        {
            return new SqliteCookieRecord
            {
                Comment = cookie.Comment ?? string.Empty,
                CommentUri = cookie.CommentUri?.AbsoluteUri,
                Discard = cookie.Discard,
                Domain = cookie.Domain ?? string.Empty,
                ExpiresBinary = cookie.Expires.ToBinary(),
                HttpOnly = cookie.HttpOnly,
                Name = cookie.Name,
                Path = cookie.Path ?? "/",
                Port = cookie.Port ?? string.Empty,
                Secure = cookie.Secure,
                Value = cookie.Value ?? string.Empty,
                Version = cookie.Version
            };
        }

        /// <summary>Builds the normalized identity used by primary keys and blind indexes.</summary>
        public string GetIdentity()
        {
            return $"{this.Domain.ToLowerInvariant()}\n{this.Path}\n{this.Name.ToLowerInvariant()}";
        }

        /// <summary>Recreates a native cookie from its persisted state.</summary>
        public Cookie ToCookie()
        {
            Cookie cookie = new(this.Name, this.Value, this.Path, this.Domain)
            {
                Comment = this.Comment,
                Discard = this.Discard,
                Expires = DateTime.FromBinary(this.ExpiresBinary),
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
