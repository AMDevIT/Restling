using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Serialization;
using AMDevIT.Restling.Tests.Codecs;
using AMDevIT.Restling.Tests.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class StatusResponseMappingTests
    {
        #region Methods

        /// <summary>Verifies the built-in HTTP status-class patterns.</summary>
        [TestMethod]
        [DataRow(199, "informational")]
        [DataRow(200, "successful")]
        [DataRow(302, "redirection")]
        [DataRow(404, "client-error")]
        [DataRow(503, "server-error")]
        public void BuiltInPatternsMatchTheirStatusClass(int numericStatusCode, string patternName)
        {
            ResponseStatusPattern pattern;

            pattern = patternName switch
            {
                "informational" => ResponseStatusPattern.Informational,
                "successful" => ResponseStatusPattern.Successful,
                "redirection" => ResponseStatusPattern.Redirection,
                "client-error" => ResponseStatusPattern.ClientError,
                "server-error" => ResponseStatusPattern.ServerError,
                _ => throw new ArgumentOutOfRangeException(nameof(patternName))
            };

            Assert.IsTrue(pattern.Matches((HttpStatusCode)numericStatusCode));
        }

        /// <summary>Verifies validation of custom HTTP status ranges.</summary>
        [TestMethod]
        public void InvalidStatusRangesAreRejected()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ResponseStatusPattern.ForRange(99, 400));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ResponseStatusPattern.ForRange(400, 600));
            Assert.ThrowsExactly<ArgumentException>(() => ResponseStatusPattern.ForRange(500, 400));
        }

        /// <summary>Verifies that an exact status mapping has priority over a broader pattern.</summary>
        [TestMethod]
        public async Task ExactStatusMappingHasPriorityOverRange()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    "{\"Id\":7,\"Name\":\"exact\"}",
                                                                    "application/json");

            request.ResponseMappings
                   .ForClientErrors<Dictionary<string, string>>()
                   .ForStatus<CodecTestModel>(HttpStatusCode.BadRequest);

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.IsNull(result.Data);
            Assert.AreEqual(typeof(CodecTestModel), result.MappedDataType);
            Assert.IsTrue(result.TryGetMappedData(out CodecTestModel? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual(7, mappedData.Id);
            Assert.AreEqual("exact", mappedData.Name);
        }

        /// <summary>Verifies first-registration priority between overlapping non-exact patterns.</summary>
        [TestMethod]
        public async Task FirstMatchingPatternWins()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.UnprocessableEntity,
                                                                    "{\"selected\":\"first\"}",
                                                                    "application/json");

            request.ResponseMappings
                   .ForErrors<Dictionary<string, string>>()
                   .ForClientErrors<CodecTestModel>();

            RestRequestResult<CodecTestModel> result = await parser.DecodeAsync<CodecTestModel>(response,
                                                                                                 request,
                                                                                                 TimeSpan.Zero);

            Assert.AreEqual(typeof(Dictionary<string, string>), result.MappedDataType);
            Assert.IsTrue(result.TryGetMappedData(out Dictionary<string, string>? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual("first", mappedData["selected"]);
        }

        /// <summary>Verifies that the latest exact registration replaces an earlier one.</summary>
        [TestMethod]
        public async Task LatestDuplicateExactMappingWins()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    "{\"Id\":9,\"Name\":\"replacement\"}",
                                                                    "application/json");

            request.ResponseMappings
                   .ForStatus<Dictionary<string, string>>(HttpStatusCode.BadRequest)
                   .ForStatus<CodecTestModel>(HttpStatusCode.BadRequest);

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.AreEqual(typeof(CodecTestModel), result.MappedDataType);
            Assert.IsTrue(result.TryGetMappedData(out CodecTestModel? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual(9, mappedData.Id);
        }

        /// <summary>Verifies fallback decoding when no exact or pattern mapping matches.</summary>
        [TestMethod]
        public async Task FallbackHandlesOtherwiseUnmatchedStatus()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.Accepted,
                                                                    "{\"Id\":3,\"Name\":\"fallback\"}",
                                                                    "application/json");

            request.ResponseMappings
                   .ForClientErrors<Dictionary<string, string>>()
                   .Fallback<CodecTestModel>();

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.IsNull(result.Data);
            Assert.IsTrue(result.TryGetMappedData(out CodecTestModel? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual("fallback", mappedData.Name);
        }

        /// <summary>Verifies that requests without mappings retain ordinary typed response decoding.</summary>
        [TestMethod]
        public async Task MissingMappingPreservesTypedDataBehavior()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.OK,
                                                                    "{\"Id\":5,\"Name\":\"ordinary\"}",
                                                                    "application/json");

            RestRequestResult<CodecTestModel> result = await parser.DecodeAsync<CodecTestModel>(response,
                                                                                                 request,
                                                                                                 TimeSpan.Zero);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual("ordinary", result.Data.Name);
            Assert.IsNull(result.MappedData);
            Assert.IsNull(result.MappedDataType);
            Assert.IsNull(result.MappedDataException);
        }

        /// <summary>Verifies mapped data on an untyped response result.</summary>
        [TestMethod]
        public async Task UntypedResultCanContainMappedData()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    "{\"Id\":11,\"Name\":\"untyped\"}",
                                                                    "application/json");

            request.ResponseMappings.ForStatus<CodecTestModel>(HttpStatusCode.BadRequest);

            RestRequestResult result = await parser.DecodeAsync(response, request, TimeSpan.Zero);

            Assert.IsTrue(result.TryGetMappedData(out CodecTestModel? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual(11, mappedData.Id);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        /// <summary>Verifies that mapped JSON decoding honors the explicitly selected serializer.</summary>
        [TestMethod]
        public async Task MappedJsonHonorsSelectedSerializer()
        {
            RestRequest systemTextRequest = CreateRequest();
            RestRequest newtonsoftRequest = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage systemTextResponse = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                              "{\"system_name\":\"system\"}",
                                                                              "application/json");
            using HttpResponseMessage newtonsoftResponse = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                              "{\"newtonsoft_name\":\"newtonsoft\"}",
                                                                              "application/json");

            systemTextRequest.ResponseMappings.ForStatus<SerializerSelectionModel>(HttpStatusCode.BadRequest);
            newtonsoftRequest.ResponseMappings.ForStatus<SerializerSelectionModel>(HttpStatusCode.BadRequest);

            RestRequestResult<string> systemTextResult = await parser.DecodeAsync<string>(systemTextResponse,
                                                                                           systemTextRequest,
                                                                                           TimeSpan.Zero,
                                                                                           PayloadJsonSerializerLibrary.SystemTextJson);
            RestRequestResult<string> newtonsoftResult = await parser.DecodeAsync<string>(newtonsoftResponse,
                                                                                           newtonsoftRequest,
                                                                                           TimeSpan.Zero,
                                                                                           PayloadJsonSerializerLibrary.NewtonsoftJson);

            Assert.IsTrue(systemTextResult.TryGetMappedData(out SerializerSelectionModel? systemTextData));
            Assert.IsTrue(newtonsoftResult.TryGetMappedData(out SerializerSelectionModel? newtonsoftData));
            Assert.IsNotNull(systemTextData);
            Assert.IsNotNull(newtonsoftData);
            Assert.AreEqual("system", systemTextData.Name);
            Assert.AreEqual("newtonsoft", newtonsoftData.Name);
        }

        /// <summary>Verifies mapped XML decoding through the registered media-type codec.</summary>
        [TestMethod]
        public async Task MappedXmlUsesXmlCodec()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    "<item><Id>13</Id><Name>xml</Name></item>",
                                                                    "application/xml");

            request.ResponseMappings.ForStatus<CodecTestModel>(HttpStatusCode.BadRequest);

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.IsTrue(result.TryGetMappedData(out CodecTestModel? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual(13, mappedData.Id);
            Assert.AreEqual("xml", mappedData.Name);
        }

        /// <summary>Verifies that mapped decoding uses a higher-priority custom codec.</summary>
        [TestMethod]
        public async Task MappedDataUsesCustomCodec()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null)
            {
                Codecs = new ContentCodecRegistry().WithCodec(new UpperCaseContentCodec())
            };
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest, "mapped", "text/plain");

            request.ResponseMappings.ForStatus<string>(HttpStatusCode.BadRequest);

            RestRequestResult<CodecTestModel> result = await parser.DecodeAsync<CodecTestModel>(response,
                                                                                                 request,
                                                                                                 TimeSpan.Zero);

            Assert.IsTrue(result.TryGetMappedData(out string? mappedData));
            Assert.AreEqual("MAPPED", mappedData);
        }

        /// <summary>Verifies mapped binary response decoding without a marker interface.</summary>
        [TestMethod]
        public async Task MappedDataSupportsByteArrays()
        {
            byte[] expectedData = [1, 2, 3, 4];
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = new(HttpStatusCode.BadRequest)
            {
                Content = new ByteArrayContent(expectedData)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            request.ResponseMappings.ForStatus<byte[]>(HttpStatusCode.BadRequest);

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.IsTrue(result.TryGetMappedData(out byte[]? mappedData));
            Assert.IsNotNull(mappedData);
            CollectionAssert.AreEqual(expectedData, mappedData);
        }

        /// <summary>Verifies that mapped decoding failures retain the complete HTTP result.</summary>
        [TestMethod]
        public async Task MappedDecodeFailurePreservesHttpResponse()
        {
            const string malformedJson = "{invalid";
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null);
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    malformedJson,
                                                                    "application/json");
            response.Headers.Add("X-Test", "retained");

            request.ResponseMappings.ForStatus<CodecTestModel>(HttpStatusCode.BadRequest);

            RestRequestResult<string> result = await parser.DecodeAsync<string>(response, request, TimeSpan.Zero);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.AreEqual(malformedJson, result.Content);
            Assert.IsNotNull(result.RawContent);
            Assert.AreEqual(typeof(CodecTestModel), result.MappedDataType);
            Assert.IsNull(result.MappedData);
            Assert.IsNotNull(result.MappedDataException);
            Assert.IsTrue(result.ResponseHeaders.Headers.ContainsKey("X-Test"));
        }

        /// <summary>Verifies that mapped data and RFC 9457 metadata can coexist.</summary>
        [TestMethod]
        public async Task MappedProblemDetailsRetainProblemMetadata()
        {
            RestRequest request = CreateRequest();
            HttpResponseParser parser = new(null)
            {
                Codecs = new ContentCodecRegistry().WithCodec(new ProblemDetailsJsonCodec())
            };
            using HttpResponseMessage response = CreateTextResponse(HttpStatusCode.BadRequest,
                                                                    "{\"title\":\"Invalid\",\"status\":400}",
                                                                    "application/problem+json");

            request.ResponseMappings.ForStatus<RestProblemDetails>(HttpStatusCode.BadRequest);

            RestRequestResult<CodecTestModel> result = await parser.DecodeAsync<CodecTestModel>(response,
                                                                                                 request,
                                                                                                 TimeSpan.Zero);

            Assert.IsTrue(result.TryGetMappedData(out RestProblemDetails? mappedData));
            Assert.IsNotNull(mappedData);
            Assert.AreEqual("Invalid", mappedData.Title);
            Assert.IsNotNull(result.Problem);
            Assert.AreEqual("Invalid", result.Problem.Title);
            Assert.IsNull(result.ProblemException);
        }

        /// <summary>Creates a request suitable for deterministic parser tests.</summary>
        private static RestRequest CreateRequest()
        {
            return new RestRequest("https://example.test/status-mapping", Core.HttpMethod.Get);
        }

        /// <summary>Creates a buffered textual HTTP response.</summary>
        private static HttpResponseMessage CreateTextResponse(HttpStatusCode statusCode,
                                                              string content,
                                                              string mediaType)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, mediaType)
            };
        }

        #endregion
    }
}
