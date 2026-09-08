using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Serialization;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>XML codec with DTD processing prohibited by default.</summary>
    public sealed class XmlContentCodec : IContentCodec
    {
        #region Properties

        public bool IsBinary => false;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            if (mediaType?.ToLowerInvariant() is "application/xml" or "text/xml" or "application/atom+xml")
                return true;

            return mediaType?.StartsWith("application/", StringComparison.OrdinalIgnoreCase) == true &&
                   mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(mediaType, "application/problem+xml", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => this.CanRead(mediaType);

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            XmlSerializer serializer = new(typeof(T));
            using StringReader input = new(context.DecodeText(content, contentType));
            if (context.AllowUnsafeXml)
                return (T?)serializer.Deserialize(input);

            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using XmlReader reader = XmlReader.Create(input, settings);
            return (T?)serializer.Deserialize(reader);
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            XmlSerializer serializer = new(typeof(T));
            XmlWriterSettings settings = new() { OmitXmlDeclaration = true };
            using StringWriter output = new();
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                serializer.Serialize(writer, value);
            }
            return context.CreateTextContent(output.ToString(), contentType);
        }

        #endregion
    }
}
