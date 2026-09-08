using System.Net;

namespace AMDevIT.Restling.Core
{
    /// <summary>Matches an inclusive range of HTTP response status codes.</summary>
    public sealed class ResponseStatusPattern
    {
        #region Properties

        /// <summary>Matches informational HTTP responses.</summary>
        public static ResponseStatusPattern Informational { get; } = new(100, 199);

        /// <summary>Matches successful HTTP responses.</summary>
        public static ResponseStatusPattern Successful { get; } = new(200, 299);

        /// <summary>Matches redirection HTTP responses.</summary>
        public static ResponseStatusPattern Redirection { get; } = new(300, 399);

        /// <summary>Matches client-error HTTP responses.</summary>
        public static ResponseStatusPattern ClientError { get; } = new(400, 499);

        /// <summary>Matches server-error HTTP responses.</summary>
        public static ResponseStatusPattern ServerError { get; } = new(500, 599);

        /// <summary>Matches client- and server-error HTTP responses.</summary>
        public static ResponseStatusPattern Error { get; } = new(400, 599);

        /// <summary>Gets the inclusive lower bound.</summary>
        public int MinimumStatusCode { get; }

        /// <summary>Gets the inclusive upper bound.</summary>
        public int MaximumStatusCode { get; }

        internal bool IsExact => this.MinimumStatusCode == this.MaximumStatusCode;

        #endregion

        #region .ctor

        private ResponseStatusPattern(int minimumStatusCode, int maximumStatusCode)
        {
            this.MinimumStatusCode = minimumStatusCode;
            this.MaximumStatusCode = maximumStatusCode;
        }

        #endregion

        #region Methods

        /// <summary>Creates a pattern matching one HTTP status code.</summary>
        /// <param name="statusCode">The HTTP status code to match.</param>
        /// <returns>A pattern matching only the supplied status code.</returns>
        public static ResponseStatusPattern ForStatus(HttpStatusCode statusCode)
        {
            int numericStatusCode = (int)statusCode;

            ValidateStatusCode(numericStatusCode, nameof(statusCode));
            return new ResponseStatusPattern(numericStatusCode, numericStatusCode);
        }

        /// <summary>Creates a pattern matching an inclusive HTTP status-code range.</summary>
        /// <param name="minimumStatusCode">The inclusive lower bound.</param>
        /// <param name="maximumStatusCode">The inclusive upper bound.</param>
        /// <returns>A pattern matching the supplied range.</returns>
        public static ResponseStatusPattern ForRange(int minimumStatusCode, int maximumStatusCode)
        {
            ValidateStatusCode(minimumStatusCode, nameof(minimumStatusCode));
            ValidateStatusCode(maximumStatusCode, nameof(maximumStatusCode));
            if (minimumStatusCode > maximumStatusCode)
                throw new ArgumentException("The minimum status code cannot exceed the maximum status code.", nameof(minimumStatusCode));

            return new ResponseStatusPattern(minimumStatusCode, maximumStatusCode);
        }

        /// <summary>Determines whether an HTTP status code is included in this pattern.</summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns>True when the status code is within the configured range.</returns>
        public bool Matches(HttpStatusCode statusCode)
        {
            int numericStatusCode = (int)statusCode;

            return numericStatusCode >= this.MinimumStatusCode && numericStatusCode <= this.MaximumStatusCode;
        }

        /// <summary>Rejects values outside the HTTP status-code range supported by mappings.</summary>
        private static void ValidateStatusCode(int statusCode, string parameterName)
        {
            if (statusCode < 100 || statusCode > 599)
                throw new ArgumentOutOfRangeException(parameterName, statusCode, "HTTP status codes must be between 100 and 599.");
        }

        #endregion
    }
}
