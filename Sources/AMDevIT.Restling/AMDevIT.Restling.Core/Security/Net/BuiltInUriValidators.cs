namespace AMDevIT.Restling.Core.Security.Net
{
    /// <summary>
    /// Defines built-in URI validators for common scenarios.
    /// </summary>
    public class BuiltInUriValidators
    {
        #region Properties

        /// <summary>
        /// Create a validator that checks if a URI doesn't 
        /// contains "localhost", "127.0.0.1" or "::1" in the host part.
        /// </summary>
        public static Func<string, bool> NoLocalHostValidator => new (UriHaveNoLocalHost);

        #endregion

        private static bool UriHaveNoLocalHost(string uri)
        {
            bool uriParsed;
            string[] defaultBlockedHosts =
            [
                "localhost", 
                "127.0.0.1", 
                "::1"
            ];

            uriParsed = System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed);

            if (uriParsed)
            {
                string? host = parsed?.Host?.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(host))
                    return !defaultBlockedHosts.Contains(host);
                else
                    return false;
            }

            return uriParsed;
        }
    }
}
