using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>An immutable, ordered codec registry. First matching codec wins.</summary>
    public sealed class ContentCodecRegistry
    {
        #region Fields

        private readonly ReadOnlyCollection<IContentCodec> codecs;

        #endregion

        #region Properties

        public IReadOnlyList<IContentCodec> Codecs => this.codecs;

        #endregion

        #region .ctor

        /// <summary>Registers JSON, XML, text, multipart and the binary fallback.</summary>
        public ContentCodecRegistry()
            : this([new JsonContentCodec(),
                    new XmlContentCodec(),
                    new TextContentCodec(),
                    new MultipartContentCodec(),
                    new BinaryContentCodec()])
        {
        }

        /// <summary>Creates a registry from an explicit ordered list, without adding defaults.</summary>
        public ContentCodecRegistry(IEnumerable<IContentCodec> codecs)
        {
            ArgumentNullException.ThrowIfNull(codecs);
            IContentCodec[] snapshot = codecs.ToArray();
            if (snapshot.Any(codec => codec == null))
                throw new ArgumentException("Codecs cannot contain null entries.", nameof(codecs));
            this.codecs = Array.AsReadOnly(snapshot);
        }

        #endregion

        #region Methods

        /// <summary>Returns a new registry with the codec before all existing registrations.</summary>
        public ContentCodecRegistry WithCodec(IContentCodec codec)
        {
            ArgumentNullException.ThrowIfNull(codec);
            return new ContentCodecRegistry(new[] { codec }.Concat(this.codecs));
        }

        /// <summary>Finds the first reader for a content type, ignoring its parameters.</summary>
        public IContentCodec? FindReader(string? contentType)
        {
            string? mediaType = NormalizeMediaType(contentType);
            return this.codecs.FirstOrDefault(codec => codec.CanRead(mediaType));
        }

        /// <summary>Finds the first writer for a content type, ignoring its parameters.</summary>
        public IContentCodec? FindWriter(string? contentType)
        {
            string? mediaType = NormalizeMediaType(contentType);
            return this.codecs.FirstOrDefault(codec => codec.CanWrite(mediaType));
        }

        /// <summary>Normalizes the media type without changing the caller's headers.</summary>
        private static string? NormalizeMediaType(string? contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? null : MediaTypeHeaderValue.Parse(contentType).MediaType?.ToLowerInvariant();
        }

        #endregion
    }
}
