namespace AMDevIT.Restling.Core.Cookies
{
    public class HttpCookieData(string name,
                                string? value,
                                string? domain = null, 
                                string? path = null,
                                bool? isSecure,
                                string? uri = null)
        : IEquatable<HttpCookieData>, IComparable<HttpCookieData>
    {
        #region Fields

        private readonly string? domain = domain;
        private readonly string? path = path;
        private readonly string? uri = uri;
        private readonly string name = name;
        private readonly bool? isSecure = isSecure;

        private string? value = value;

        #endregion

        #region Properties

        public string? Domain => this.domain;
        public string? Path => this.path;
        public string? Uri => this.uri;
        public bool? IsSecure => this.isSecure;
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
            return $"[Domain:{this.Domain},Path:{this.path},Uri:{this.Uri}, IsSecure:{this.IsSecure}]{this.Name}={this.Value}";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as HttpCookieData);
        }

        public bool Equals(HttpCookieData? other)
        {
            if (ReferenceEquals(this, other)) 
                return true;

            if (other == null) 
                return false;

            return string.Equals(this.name, other.name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.domain, other.domain, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.path, other.path, StringComparison.Ordinal) &&
                   string.Equals(this.uri, other.uri, StringComparison.Ordinal) &&
                   bool.Equals(this.isSecure, other.isSecure);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.name?.ToLowerInvariant(),
                this.domain?.ToLowerInvariant(),
                this.path,
                this.uri,
                this.isSecure.GetValueOrDefault()
            );
        }

        public int CompareTo(HttpCookieData? other)
        {
            if (other == null) 
                return 1;

            int nameComparison = string.Compare(this.name, other.name, StringComparison.OrdinalIgnoreCase);

            if (nameComparison != 0) 
                return nameComparison;

            int domainComparison = string.Compare(this.domain, other.domain, StringComparison.OrdinalIgnoreCase);
            if (domainComparison != 0) 
                return domainComparison;

            int pathComparison = string.Compare(this.path, other.path, StringComparison.Ordinal);
            if (pathComparison != 0) 
                return pathComparison;

            int isSecureComparison = (this.isSecure ?? false).CompareTo(other.isSecure ?? false);
            if (isSecureComparison != 0) 
                return isSecureComparison;


            return string.Compare(this.uri, other.uri, StringComparison.Ordinal);
        }

        #region Operators

        public static bool operator ==(HttpCookieData? left, HttpCookieData? right)
        {
            if (left is null) 
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(HttpCookieData? left, HttpCookieData? right)
        {
            return !(left == right);
        }

        #endregion

        #endregion
    }
}
