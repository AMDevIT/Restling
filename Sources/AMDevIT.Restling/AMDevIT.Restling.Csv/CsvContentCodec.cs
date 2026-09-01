using AMDevIT.Restling.Core.Codecs;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections;
using System.Globalization;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Csv
{
    /// <summary>Optional buffered CSV codec for arrays, lists and standard collection interfaces.</summary>
    public sealed class CsvContentCodec : IContentCodec
    {
        #region Fields

        private readonly CsvContentCodecOptions options;
        private readonly CultureInfo culture;

        #endregion

        #region Properties

        public bool IsBinary => false;

        #endregion

        #region .ctor

        /// <summary>Creates a CSV codec using invariant culture, comma separation and headers by default.</summary>
        public CsvContentCodec(CsvContentCodecOptions? options = null)
        {
            this.options = options ?? new CsvContentCodecOptions();
            ArgumentNullException.ThrowIfNull(this.options.Culture);
            ArgumentException.ThrowIfNullOrEmpty(this.options.Delimiter);
            this.culture = CultureInfo.ReadOnly((CultureInfo)this.options.Culture.Clone());
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            return string.Equals(mediaType, "text/csv", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => this.CanRead(mediaType);

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            Type targetType = typeof(T);
            Type recordType = GetRecordType(targetType);
            IList records = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(recordType))!;
            string text = context.DecodeText(content, contentType).TrimStart('\uFEFF');
            using StringReader input = new(text);
            using CsvReader reader = new(input, this.CreateConfiguration());
            this.options.ConfigureContext?.Invoke(reader.Context);

            foreach (object record in reader.GetRecords(recordType))
                records.Add(record);

            if (targetType.IsArray)
            {
                Array array = Array.CreateInstance(recordType, records.Count);
                records.CopyTo(array, 0);
                return (T)(object)array;
            }
            return (T)(object)records;
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            if (value is not IEnumerable records || value is string || value is byte[])
                throw new ArgumentException("CSV serialization requires a collection of records.", nameof(value));

            using StringWriter output = new(this.culture);
            using (CsvWriter writer = new(output, this.CreateConfiguration()))
            {
                this.options.ConfigureContext?.Invoke(writer.Context);
                writer.WriteRecords(records);
            }
            return context.CreateTextContent(output.ToString(), contentType);
        }

        /// <summary>Creates independent settings for each operation.</summary>
        private CsvConfiguration CreateConfiguration()
        {
            CsvConfiguration configuration = new(this.culture)
            {
                Delimiter = this.options.Delimiter,
                HasHeaderRecord = this.options.HasHeaderRecord,
                ExceptionMessagesContainRawData = false
            };
            this.options.Configure?.Invoke(configuration);
            return configuration;
        }

        /// <summary>Resolves supported collection targets, rejecting ambiguous single-record models.</summary>
        private static Type GetRecordType(Type targetType)
        {
            if (targetType.IsArray && targetType.GetArrayRank() == 1)
                return targetType.GetElementType()!;

            if (targetType.IsGenericType)
            {
                Type definition = targetType.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(IEnumerable<>) ||
                    definition == typeof(ICollection<>) || definition == typeof(IList<>) ||
                    definition == typeof(IReadOnlyCollection<>) || definition == typeof(IReadOnlyList<>))
                    return targetType.GetGenericArguments()[0];
            }
            throw new NotSupportedException("CSV responses require TRecord[], List<TRecord> or a standard collection interface.");
        }

        #endregion
    }
}
