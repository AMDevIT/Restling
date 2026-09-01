using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace AMDevIT.Restling.Csv
{
    /// <summary>CSV settings. Callbacks receive fresh per-operation configuration/context instances.</summary>
    public sealed class CsvContentCodecOptions
    {
        #region Properties

        public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
        public string Delimiter { get; init; } = ",";
        public bool HasHeaderRecord { get; init; } = true;

        /// <summary>Configures validation, quoting and other CsvHelper settings. Must be safe for concurrent calls.</summary>
        public Action<CsvConfiguration>? Configure { get; init; }

        /// <summary>Registers maps or converters per operation. Must be safe for concurrent calls.</summary>
        public Action<CsvContext>? ConfigureContext { get; init; }

        #endregion
    }
}
