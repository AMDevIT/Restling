using AMDevIT.Restling.Core.Common;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Core.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;

namespace AMDevIT.Restling.Core
{
    internal class HttpResponseParser(ILogger? logger)
    {
        #region Consts



        #endregion

        #region Fields

        private readonly ILogger? logger = logger;

        #endregion

        #region Properties  

        protected ILogger? Logger => this.logger;

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
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                retrievedContent = RetrieveContent(rawContent, contentType);

                restRequestResult = new(restRequest,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent,
                                        contentType?.MediaType,
                                        charset,
                                        retrievedContent);
            }
            else
            {
                HttpClientException httpClientException = new("Http response object is null");
                restRequestResult = new(restRequest, httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

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
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                content = RetrieveContent(rawContent, contentType);

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
                catch (Exception)
                {
                    if (resultHttpMessage.IsSuccessStatusCode)
                        throw;
                    // If not, ignore the exception.
                }             

                restRequestResult = new(restRequest,
                                        data,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent,
                                        contentType?.MediaType,
                                        charset,
                                        content);
            }
            else
            {
                HttpClientException httpClientException = new("Http response object is null");
                restRequestResult = new(restRequest, httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

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


        private T? DecodeData<T>(byte[] rawContent, 
                                 RetrievedContentResult content, 
                                 MediaTypeHeaderValue? contentType,
                                 PayloadJsonSerializerLibrary? payloadJsonSerializerLibrary = null)
        {
            T? data = default;

            // Try decoding data.
            switch (contentType?.MediaType)
            {
                case HttpMediaType.ApplicationJson:
                    try
                    {
                        if (content.Content is string json)
                        {
                            JsonSerialization jsonSerialization = new(this.Logger);
                            data = jsonSerialization.Deserialize<T>(json, payloadJsonSerializerLibrary);
                        }
                    }
                    catch (Exception exc)
                    {
                        this.Logger?.LogError(exc, "Failed to deserialize JSON content.");
                    }
                    break;

                case HttpMediaType.ApplicationAtomXml:
                case HttpMediaType.ApplicationXml:
                case HttpMediaType.TextXml:
                    try
                    {
                        if (content.Content is string xml)
                        {
                            XmlSerializer serializer = new(typeof(T));
                            using var stringReader = new StringReader(xml);
                            data = (T?)serializer.Deserialize(stringReader);
                        }
                    }
                    catch (Exception exc)
                    {
                        this.Logger?.LogError(exc, "Failed to deserialize XML content.");
                    }
                    break;

                default:
                    // If it's a string or other primitive, try to parse it.
                    data = this.RetrievePrimitiveType<T>(rawContent, content, contentType);
                    break;

            }

            return data;
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

        private static RetrievedContentResult RetrieveContent(byte[] rawContent,
                                                                      MediaTypeHeaderValue? contentType)
        {
            object? content;
            bool isBinaryData = false;
            RetrievedContentResult contentResult;

            if (contentType == null)
            {
                content = rawContent;
            }
            else
            {
                Charset charset = CharsetParser.Parse(contentType.CharSet);
                switch (contentType.MediaType)
                {
                    case HttpMediaType.ApplicationJson:
                    case HttpMediaType.ApplicationXml:
                    case HttpMediaType.TextXml:
                    case HttpMediaType.TextPlain:
                    case HttpMediaType.TextHtml:
                    case HttpMediaType.TextCss:
                    case HttpMediaType.TextJavascript:
                    case HttpMediaType.ImageSvgXml:
                    case HttpMediaType.ApplicationAtomXml:
                        {
                            string stringContent = DecodeContentString(rawContent, charset);
                            content = stringContent;
                            isBinaryData = false;
                        }
                        break;

                    case HttpMediaType.ImagePng:
                    case HttpMediaType.ImageJpeg:
                    case HttpMediaType.ImageGif:
                    case HttpMediaType.ImageBmp:
                    case HttpMediaType.ImageWebp:
                    case HttpMediaType.ApplicationOctetStream:
                    case HttpMediaType.VideoMp4:
                    case HttpMediaType.VideoMpeg:
                    case HttpMediaType.VideoOgg:
                    case HttpMediaType.VideoWebm:
                    case HttpMediaType.VideoQuicktime:
                        {
                            content = rawContent;
                            isBinaryData = true;
                        }
                        break;

                    default:
                        {
                            // This is a fallback, if the content type is not recognized.
                            // The content is returned as a byte array.
                            content = rawContent;
                            isBinaryData = true;
                        }
                        break;
                }
            }

            contentResult = new(content, isBinaryData, contentType);
            return contentResult;
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
