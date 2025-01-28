using AMDevIT.Restling.Core.Common;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        public async Task<RestRequestResult<T>> DecodeAsync<T>(HttpResponseMessage resultHttpMessage,            
                                                               RestRequest restRequest,
                                                               TimeSpan elapsed,
                                                               CancellationToken cancellationToken = default)
        {
            RestRequestResult<T> restRequestResult;

            if (resultHttpMessage != null)
            {
                byte[] rawContent;
                T? data = default;
                string? content;
                MediaTypeHeaderValue? contentType = resultHttpMessage.Content.Headers.ContentType;
                Charset charset = CharsetParser.Parse(contentType?.CharSet);
                rawContent = await resultHttpMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                content = DecodeContentString(rawContent, charset);

                // Response received.
                if (resultHttpMessage.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(byte[]))                    
                        data = (T)(object)rawContent;                    
                    else
                    {
                        // Try decoding data.
                        switch (contentType?.MediaType)
                        {
                            case HttpMediaType.ApplicationJson:
                                try
                                {
                                    data = JsonConvert.DeserializeObject<T>(content);
                                }
                                catch(Exception exc)
                                {
                                    this.Logger?.LogError(exc, "Failed to deserialize JSON content.");
                                }
                                break;

                            case HttpMediaType.ApplicationXml:
                            case HttpMediaType.TextXml:
                                try
                                {
                                    XmlSerializer serializer = new (typeof(T));
                                    using var stringReader = new StringReader(content);
                                    data = (T?)serializer.Deserialize(stringReader);
                                }
                                catch(Exception exc)
                                {
                                    this.Logger?.LogError(exc, "Failed to deserialize XML content.");
                                }
                                break;

                            default:
                                // If it's a string or other primitive, try to parse it.
                                if (typeof(T) == typeof(string))                                
                                    data = (T)(object)content;                                
                                else if (typeof(T).IsPrimitive)
                                {
                                    try
                                    {
                                        data = (T)Convert.ChangeType(content, typeof(T));
                                    }
                                    catch (Exception convertEx)
                                    {
                                        this.Logger?.LogError(convertEx, "Failed to convert content to primitive type.");
                                    }
                                }
                                else
                                {
                                    // Unsupported content type or decoding type.
                                    this.Logger?.LogWarning("Unsupported media type: {MediaType}", contentType?.MediaType);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    // Maybe it's good to decode the content anyway.
                }

                restRequestResult = new(data,
                                        resultHttpMessage.StatusCode,
                                        elapsed,
                                        rawContent,
                                        contentType?.MediaType,
                                        charset,
                                        content);
            }
            else
            {
                HttpClientException httpClientException = new HttpClientException("Http response object is null");
                restRequestResult = new(httpClientException, elapsed);
                this.Logger?.LogError(httpClientException, "Http response object is null");
            }

            return restRequestResult;
        }

        private static string DecodeContentString(byte[] rawContent, Charset charset)
        {
            string result;

            switch (charset)
            {
                case Charset.UTF8:
                    result = Encoding.UTF8.GetString(rawContent);
                    break;
                case Charset.UTF16:
                    result = Encoding.Unicode.GetString(rawContent);
                    break;
                case Charset.UTF32:
                    result = Encoding.UTF32.GetString(rawContent);
                    break;
                case Charset.ASCII:
                    result = Encoding.ASCII.GetString(rawContent);
                    break;
                case Charset.ISO_8859_1:
                    result = Encoding.GetEncoding("iso-8859-1").GetString(rawContent);
                    break;
                case Charset.WINDOWS_1252:
                    result = Encoding.GetEncoding("windows-1252").GetString(rawContent);
                    break;
                default:
                    result = Encoding.UTF8.GetString(rawContent);
                    break;
            }

            return result;
        }

        #endregion
    }
}
