using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AMDevIT.Restling.Tests.Diagnostics
{
    public class TraceLogger<T> : ILogger<T>
    {
        #region Fields

        private readonly string categoryName;

        #endregion

        #region .ctor

        public TraceLogger()
        {
            this.categoryName = typeof(T).FullName ?? "DefaultCategory";
        }

        #endregion

        #region Methods

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            // As a simple logger, we don't need to implement this.
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // All levels are enabled for the tests.
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) return;

            var logMessage = formatter(state, exception);
            var output = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{categoryName}] {logMessage}";

            if (exception != null)
            {
                output += $"{Environment.NewLine}Exception: {exception}";
            }

            Trace.WriteLine(output);
        }

        #endregion
    }
}
