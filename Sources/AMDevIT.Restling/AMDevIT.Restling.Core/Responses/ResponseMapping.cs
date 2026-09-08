using AMDevIT.Restling.Core.Codecs;
using System.Net.Http.Headers;

namespace AMDevIT.Restling.Core
{
    internal sealed class ResponseMapping
    {
        #region Fields

        private readonly Func<IContentCodec, byte[], MediaTypeHeaderValue?, ContentCodecContext, object?> decoder;

        #endregion

        #region Properties

        public ResponseStatusPattern? Pattern { get; }

        public Type DataType { get; }

        #endregion

        #region .ctor

        private ResponseMapping(ResponseStatusPattern? pattern,
                                Type dataType,
                                Func<IContentCodec, byte[], MediaTypeHeaderValue?, ContentCodecContext, object?> decoder)
        {
            this.Pattern = pattern;
            this.DataType = dataType;
            this.decoder = decoder;
        }

        #endregion

        #region Methods

        /// <summary>Creates a mapping retaining a strongly typed codec invocation.</summary>
        public static ResponseMapping Create<T>(ResponseStatusPattern? pattern)
        {
            return new ResponseMapping(pattern,
                                       typeof(T),
                                       (codec, content, contentType, context) => codec.Deserialize<T>(content, contentType, context));
        }

        /// <summary>Decodes buffered content through the registered generic data type.</summary>
        public object? Decode(IContentCodec codec,
                              byte[] content,
                              MediaTypeHeaderValue? contentType,
                              ContentCodecContext context)
        {
            return this.decoder(codec, content, contentType, context);
        }

        #endregion
    }
}
