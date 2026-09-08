using AMDevIT.Restling.Core.Common;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Core.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Immutable per-operation settings shared with content codecs.</summary>
    public sealed class ContentCodecContext
    {
        #region Properties

        public PayloadJsonSerializerLibrary? JsonSerializerLibrary { get; init; }
        public ILogger? Logger { get; init; }
        public bool AllowUnsafeXml { get; init; }
        public ContentCodecRegistry? Codecs { get; init; }

        #endregion

        #region Methods

        /// <summary>Decodes text using Restling's existing charset rules.</summary>
        public string DecodeText(byte[] content, MediaTypeHeaderValue? contentType)
        {
            ArgumentNullException.ThrowIfNull(content);
            return this.GetEncoding(contentType).GetString(content);
        }

        /// <summary>Resolves an encoding, retaining UTF-8 as the legacy fallback.</summary>
        public Encoding GetEncoding(MediaTypeHeaderValue? contentType)
        {
            return CharsetParser.Parse(contentType?.CharSet) switch
            {
                Charset.UTF16 => Encoding.Unicode,
                Charset.UTF32 => Encoding.UTF32,
                Charset.ASCII => Encoding.ASCII,
                Charset.ISO_8859_1 => Encoding.Latin1,
                Charset.WINDOWS_1252 => Encoding.GetEncoding("windows-1252"),
                _ => Encoding.UTF8
            };
        }

        /// <summary>Creates text content and preserves media-type parameters.</summary>
        public HttpContent CreateTextContent(string text, MediaTypeHeaderValue contentType)
        {
            Encoding encoding = this.GetEncoding(contentType);
            StringContent content = new(text, encoding);
            MediaTypeHeaderValue headers = MediaTypeHeaderValue.Parse(contentType.ToString());
            headers.CharSet ??= encoding.WebName;
            content.Headers.ContentType = headers;
            return content;
        }

        #endregion
    }
}
