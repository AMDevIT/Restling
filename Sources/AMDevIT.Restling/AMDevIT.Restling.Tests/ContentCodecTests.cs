using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Csv;
using AMDevIT.Restling.Tests.Codecs;
using AMDevIT.Restling.Tests.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class ContentCodecTests
    {
        #region Methods

        /// <summary>Verifies the codecs available without explicit configuration.</summary>
        [TestMethod]
        public void DefaultRegistryContainsBackwardCompatibleCodecs()
        {
            ContentCodecRegistry registry = new();

            Assert.IsInstanceOfType<JsonContentCodec>(registry.FindReader("application/json"));
            Assert.IsInstanceOfType<JsonContentCodec>(registry.FindReader("application/hal+json"));
            Assert.IsInstanceOfType<XmlContentCodec>(registry.FindReader("application/xml"));
            Assert.IsInstanceOfType<TextContentCodec>(registry.FindReader("text/plain"));
            Assert.IsInstanceOfType<MultipartContentCodec>(registry.FindReader("multipart/mixed"));
            Assert.IsInstanceOfType<BinaryContentCodec>(registry.FindReader("image/png"));
            Assert.IsInstanceOfType<BinaryContentCodec>(registry.FindWriter("image/png"));
            Assert.IsNull(registry.Codecs.OfType<CsvContentCodec>().SingleOrDefault());
            Assert.IsNull(registry.Codecs.OfType<ProblemDetailsJsonCodec>().SingleOrDefault());
            Assert.IsInstanceOfType<BinaryContentCodec>(registry.FindReader("application/problem+json"));
        }

        /// <summary>Verifies first-match priority for custom codecs.</summary>
        [TestMethod]
        public void EarlierCustomCodecOverridesDefault()
        {
            ContentCodecRegistry registry = new ContentCodecRegistry().WithCodec(new UpperCaseContentCodec());
            IContentCodec codec = registry.FindReader("text/plain")!;
            string? result = codec.Deserialize<string>(Encoding.UTF8.GetBytes("hello"),
                                                       new MediaTypeHeaderValue("text/plain"),
                                                       new ContentCodecContext());

            Assert.AreEqual("HELLO", result);
        }

        /// <summary>Verifies the built-in structured text codecs.</summary>
        [TestMethod]
        public void JsonAndXmlRoundTripModels()
        {
            CodecTestModel model = new() { Id = 7, Name = "Restling" };
            ContentCodecContext context = new();

            this.AssertRoundTrip(new JsonContentCodec(), model, "application/json", context);
            this.AssertRoundTrip(new XmlContentCodec(), model, "application/xml", context);
        }

        /// <summary>Verifies optional CSV list serialization and deserialization.</summary>
        [TestMethod]
        public void CsvRoundTripsLists()
        {
            List<CodecTestModel> models = [new CodecTestModel { Id = 7, Name = "Restling" }];
            ContentCodecContext context = new();
            CsvContentCodec codec = new();
            MediaTypeHeaderValue mediaType = new("text/csv");
            HttpContent encoded = codec.Serialize(models, mediaType, context);
            byte[] bytes = encoded.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            List<CodecTestModel>? decoded = codec.Deserialize<List<CodecTestModel>>(bytes, mediaType, context);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(7, decoded[0].Id);
            Assert.AreEqual("Restling", decoded[0].Name);
        }

        /// <summary>Verifies RFC 9457 member validation and extensions.</summary>
        [TestMethod]
        public void ProblemCodecPreservesExtensionsAndIgnoresWrongStandardTypes()
        {
            const string json = "{\"type\":42,\"title\":\"Invalid\",\"status\":400,\"trace_id\":\"abc\"}";
            ProblemDetailsJsonCodec codec = new();
            RestProblemDetails? problem = codec.DeserializeProblem(Encoding.UTF8.GetBytes(json),
                                                                   new MediaTypeHeaderValue("application/problem+json"),
                                                                   new ContentCodecContext());

            Assert.IsNotNull(problem);
            Assert.AreEqual("about:blank", problem.Type);
            Assert.AreEqual("Invalid", problem.Title);
            Assert.AreEqual(400, problem.Status);
            Assert.AreEqual("abc", problem.Extensions["trace_id"].GetString());
        }

#if DEBUG
        /// <summary>Verifies that a problem document does not populate the success model.</summary>
        [TestMethod]
        public async Task ParserKeepsProblemSeparateFromSuccessData()
        {
            HttpResponseMessage response = new(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"title\":\"Invalid\",\"status\":400}", Encoding.UTF8, "application/problem+json")
            };
            HttpResponseParser parser = new(null)
            {
                Codecs = new ContentCodecRegistry().WithCodec(new ProblemDetailsJsonCodec())
            };
            RestRequest request = new("https://example.test", Core.HttpMethod.Get);
            RestRequestResult<CodecTestModel> result = await parser.DecodeAsync<CodecTestModel>(response,
                                                                                                request,
                                                                                                TimeSpan.Zero);

            Assert.IsNull(result.Data);
            Assert.IsNotNull(result.Problem);
            Assert.AreEqual("Invalid", result.Problem.Title);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }
#endif

        /// <summary>Verifies a codec through its public request/response contract.</summary>
        private void AssertRoundTrip(IContentCodec codec,
                                     CodecTestModel model,
                                     string mediaType,
                                     ContentCodecContext context)
        {
            MediaTypeHeaderValue contentType = new(mediaType);
            HttpContent encoded = codec.Serialize(model, contentType, context);
            byte[] bytes = encoded.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            CodecTestModel? decoded = codec.Deserialize<CodecTestModel>(bytes, contentType, context);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(model.Id, decoded.Id);
            Assert.AreEqual(model.Name, decoded.Name);
        }

        #endregion
    }
}
