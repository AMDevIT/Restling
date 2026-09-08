using AMDevIT.Restling.Core.Common;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Core.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Core
{
    internal class HttpResponseParser(ILogger? logger)
    {
        #region Fields

        private readonly ILogger? logger = logger;

        #endregion

        #region Properties  

        protected ILogger? Logger => this.logger;

        public ContentCodecRegistry Codecs { get; init; } = new();

        public bool AllowUnsafeXml { get; set; } = false;

        public bool ThrowOnDecodeError { get; set; } = false;

        public bool LogOnDecodeErrorIfNotSuccess { get; set; } = false;

        #endregion

        #region Methods

        public async Task<RestRequestResult> DecodeAsync(HttpResponseMessage resultHttpMessage,
                                                         RestRequest restRequest,
                                                         TimeSpan elapsed,
                                                         CancellationToken cancellationToken = default)
        {
            RestRequestResult restRequestResult;

            if (resultHttpMessage != null)
            {
                byte[] rawContent;
                RetrievedContentResult retrievedContent;
                ResponseHeaders responseHeaders;
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                retrievedContent = RetrieveContent(rawContent, contentType);
                
                responseHeaders = ResponseHeaders.Create(resultHttpMessage.Headers);

                restRequestResult = new(restRequest,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent,
                                        contentType?.MediaType,
                                        charset,
                                        retrievedContent,
                                        responseHeaders);
            }
            else
            {
                HttpClientException httpClientException = new("Http response object is null");
                restRequestResult = new(restRequest, httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

            this.AttachProblem(restRequestResult);
            return restRequestResult;
        }


        public async Task<RestRequestResult<T>> DecodeAsync<T>(HttpResponseMessage resultHttpMessage,
                                                               RestRequest restRequest,
                                                               TimeSpan elapsed,
                                                               PayloadJsonSerializerLibrary? payloadJsonSerializerLibrary = null,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequestResult<T> restRequestResult;

            if (resultHttpMessage != null)
            {
                byte[] rawContent;
                T? data = default;
                RetrievedContentResult content;
                ResponseHeaders responseHeaders;
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                content = RetrieveContent(rawContent, contentType);
                Exception? decodeException = null;

                // Response received.

                try
                {
                    data = typeof(T) switch
                    {
                        Type t when t == typeof(byte[]) => (T)(object)rawContent,

                        Type t when t == typeof(string) => (T)(object)(
                            content.IsBinaryData == true
                                ? Convert.ToBase64String(rawContent)
                                : content.Content?.ToString() ?? string.Empty
                        ),
                        Type t when t.IsPrimitive => ConvertPrimitive<T>(content.Content),
                        _ => this.DecodeData<T>(rawContent, content, contentType, payloadJsonSerializerLibrary: payloadJsonSerializerLibrary)
                    };
                }
                catch (Exception exception)
                {
                    if (resultHttpMessage.IsSuccessStatusCode)
                    {
                        if (this.ThrowOnDecodeError)
                            throw;
                        else
                            decodeException = exception;
                    }
                    else
                    {
                        if (this.LogOnDecodeErrorIfNotSuccess)
                        {
                            // If the request failed, we don't want to throw an exception for decoding.
                            // We just log it and return the default value.
                            this.Logger?.LogError(exception, "Failed to decode content.");
                        }
                    }
                }             

                responseHeaders = ResponseHeaders.Create(resultHttpMessage.Headers);
                restRequestResult = new(restRequest,
                                        data,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent,
                                        contentType?.MediaType,
                                        charset,
                                        content,
                                        responseHeaders,
                                        exception: decodeException);
            }
            else
            {
                HttpClientException httpClientException = new("Http response object is null");
                restRequestResult = new(restRequest, httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

            this.AttachProblem(restRequestResult);
            return restRequestResult;
        }

        private T? RetrievePrimitiveType<T>(byte[] rawContent, RetrievedContentResult content, MediaTypeHeaderValue? contentType)
        {
            T? data = typeof(T) switch
            {
                Type t when t == typeof(byte[]) => (T)(object)rawContent,

                Type t when t == typeof(string) => (T)(object)(
                    content.IsBinaryData == true
                        ? Convert.ToBase64String(rawContent)
                        : content.Content?.ToString() ?? string.Empty
                ),

                Type t when t.IsPrimitive => ConvertPrimitive<T>(content.Content),

                _ => this.LogAndReturnDefault<T>(contentType?.MediaType)
            };

            return data;
        }


        /// <summary>Decodes a model using the registry while retaining legacy JSON error handling.</summary>
        private T? DecodeData<T>(byte[] rawContent,
                                 RetrievedContentResult content,
                                 MediaTypeHeaderValue? contentType,
                                 PayloadJsonSerializerLibrary? payloadJsonSerializerLibrary = null)
        {
            IContentCodec? codec = this.Codecs.FindReader(contentType?.MediaType);
            ContentCodecContext context = new()
            {
                Logger = this.Logger,
                JsonSerializerLibrary = payloadJsonSerializerLibrary,
                AllowUnsafeXml = this.AllowUnsafeXml,
                Codecs = this.Codecs
            };
            if (codec is IProblemDetailsCodec && typeof(T) != typeof(RestProblemDetails))
                return default;
            if (codec == null)
                return this.RetrievePrimitiveType<T>(rawContent, content, contentType);

            try
            {
                return codec.Deserialize<T>(rawContent, contentType, context);
            }
            catch (Exception exception) when (codec is JsonContentCodec)
            {
                // Retain the legacy JSON default-on-error behavior.
                this.Logger?.LogError(exception, "Failed to deserialize JSON content.");
                return default;
            }
        }

        /// <summary>Attaches optional problem metadata without replacing the HTTP result.</summary>
        private void AttachProblem(RestRequestResult result)
        {
            IContentCodec? codec = this.Codecs.FindReader(result.ContentType);
            if (codec is not IProblemDetailsCodec problemCodec || result.RawContent == null)
                return;

            try
            {
                ContentCodecContext context = new() { Logger = this.Logger, Codecs = this.Codecs };
                result.Problem = problemCodec.DeserializeProblem(result.RawContent, result.RetrievedContent?.ContentType, context);
            }
            catch (Exception exception)
            {
                result.ProblemException = exception;
            }
        }


        private T? ConvertPrimitive<T>(object? content)
        {
            try
            {
                return content != null ? (T)Convert.ChangeType(content, typeof(T)) : default;
            }
            catch (Exception convertEx)
            {
                // Assumendo che Logger sia disponibile nel contesto
                this.Logger?.LogError(convertEx, "Failed to convert content to primitive type.");
                return default;
            }
        }

        private T? LogAndReturnDefault<T>(string? mediaType)
        {
            this.Logger?.LogWarning("Unsupported media type: {MediaType}", mediaType);
            return default;
        }

        /// <summary>Classifies the original body through the selected codec, preserving the missing-header fallback.</summary>
        private RetrievedContentResult RetrieveContent(byte[] rawContent, MediaTypeHeaderValue? contentType)
        {
            if (contentType == null)
                return new RetrievedContentResult(rawContent, false, null);

            IContentCodec? codec = this.Codecs.FindReader(contentType.MediaType);
            bool isBinary = codec?.IsBinary ?? true;
            object content = isBinary ? rawContent : DecodeContentString(rawContent, CharsetParser.Parse(contentType.CharSet));
            return new RetrievedContentResult(content, isBinary, contentType);
        }

        private static string DecodeContentString(byte[] rawContent, Charset charset)
        {
            string result = charset switch
            {
                Charset.UTF8 => Encoding.UTF8.GetString(rawContent),
                Charset.UTF16 => Encoding.Unicode.GetString(rawContent),
                Charset.UTF32 => Encoding.UTF32.GetString(rawContent),
                Charset.ASCII => Encoding.ASCII.GetString(rawContent),
                Charset.ISO_8859_1 => Encoding.GetEncoding("iso-8859-1").GetString(rawContent),
                Charset.WINDOWS_1252 => Encoding.GetEncoding("windows-1252").GetString(rawContent),
                _ => Encoding.UTF8.GetString(rawContent),
            };
            return result;
        }


        #endregion
    }
}
