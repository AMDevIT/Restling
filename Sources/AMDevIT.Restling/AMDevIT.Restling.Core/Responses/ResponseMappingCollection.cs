using System.Net;

namespace AMDevIT.Restling.Core
{
    /// <summary>Registers response data types selected by HTTP status code.</summary>
    public sealed class ResponseMappingCollection
    {
        #region Fields

        private readonly object syncRoot = new();
        private ResponseMapping[] mappings = [];
        private ResponseMapping? fallback;

        #endregion

        #region Methods

        /// <summary>Maps one HTTP status code to a response data type.</summary>
        /// <typeparam name="T">The response data type.</typeparam>
        /// <param name="statusCode">The HTTP status code to map.</param>
        /// <returns>The current mapping collection.</returns>
        public ResponseMappingCollection ForStatus<T>(HttpStatusCode statusCode)
        {
            return this.ForPattern<T>(ResponseStatusPattern.ForStatus(statusCode));
        }

        /// <summary>Maps an inclusive HTTP status-code range to a response data type.</summary>
        /// <typeparam name="T">The response data type.</typeparam>
        /// <param name="minimumStatusCode">The inclusive lower bound.</param>
        /// <param name="maximumStatusCode">The inclusive upper bound.</param>
        /// <returns>The current mapping collection.</returns>
        public ResponseMappingCollection ForRange<T>(int minimumStatusCode, int maximumStatusCode)
        {
            return this.ForPattern<T>(ResponseStatusPattern.ForRange(minimumStatusCode, maximumStatusCode));
        }

        /// <summary>Maps a reusable HTTP status pattern to a response data type.</summary>
        /// <typeparam name="T">The response data type.</typeparam>
        /// <param name="pattern">The HTTP status pattern to map.</param>
        /// <returns>The current mapping collection.</returns>
        public ResponseMappingCollection ForPattern<T>(ResponseStatusPattern pattern)
        {
            ResponseMapping mapping;

            ArgumentNullException.ThrowIfNull(pattern);
            mapping = ResponseMapping.Create<T>(pattern);
            lock (this.syncRoot)
            {
                if (pattern.IsExact)
                {
                    this.mappings = this.mappings
                        .Where(current => current.Pattern == null ||
                                          !current.Pattern.IsExact ||
                                          current.Pattern.MinimumStatusCode != pattern.MinimumStatusCode)
                        .Append(mapping)
                        .ToArray();
                }
                else
                    this.mappings = this.mappings.Append(mapping).ToArray();
            }
            return this;
        }

        /// <summary>Maps informational HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForInformational<T>() => this.ForPattern<T>(ResponseStatusPattern.Informational);

        /// <summary>Maps successful HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForSuccessful<T>() => this.ForPattern<T>(ResponseStatusPattern.Successful);

        /// <summary>Maps redirection HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForRedirections<T>() => this.ForPattern<T>(ResponseStatusPattern.Redirection);

        /// <summary>Maps client-error HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForClientErrors<T>() => this.ForPattern<T>(ResponseStatusPattern.ClientError);

        /// <summary>Maps server-error HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForServerErrors<T>() => this.ForPattern<T>(ResponseStatusPattern.ServerError);

        /// <summary>Maps client- and server-error HTTP responses to a response data type.</summary>
        public ResponseMappingCollection ForErrors<T>() => this.ForPattern<T>(ResponseStatusPattern.Error);

        /// <summary>Maps any otherwise unmatched HTTP response to a response data type.</summary>
        /// <typeparam name="T">The response data type.</typeparam>
        /// <returns>The current mapping collection.</returns>
        public ResponseMappingCollection Fallback<T>()
        {
            lock (this.syncRoot)
            {
                this.fallback = ResponseMapping.Create<T>(pattern: null);
            }
            return this;
        }

        /// <summary>Finds the highest-precedence mapping for an HTTP status code.</summary>
        internal ResponseMapping? Find(HttpStatusCode statusCode)
        {
            ResponseMapping[] mappings;
            ResponseMapping? fallback;
            ResponseMapping? exactMapping;

            lock (this.syncRoot)
            {
                mappings = this.mappings;
                fallback = this.fallback;
            }

            exactMapping = mappings.FirstOrDefault(mapping => mapping.Pattern?.IsExact == true &&
                                                              mapping.Pattern.Matches(statusCode));
            return exactMapping ??
                   mappings.FirstOrDefault(mapping => mapping.Pattern?.IsExact == false &&
                                                      mapping.Pattern.Matches(statusCode)) ??
                   fallback;
        }

        #endregion
    }
}
