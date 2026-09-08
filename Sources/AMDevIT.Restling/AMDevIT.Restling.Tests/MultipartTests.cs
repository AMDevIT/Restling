using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Codecs;
using AMDevIT.Restling.Core.Multipart;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Tests.Multipart;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using RestlingHttpMethod = AMDevIT.Restling.Core.HttpMethod;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class MultipartTests
    {
        #region Methods

        /// <summary>Verifies ordered duplicate fields, binary data and per-part codec decoding.</summary>
        [TestMethod]
        public void MultipartCodecPreservesPartsAndDecodesTheirContent()
        {
            const string body = "preamble\r\n--sample\r\nContent-Disposition: form-data; name=\"value\"\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nfirst\r\n--sample\r\nContent-Disposition: form-data; name=\"value\"\r\nContent-Type: application/json\r\n\r\n{\"name\":\"second\"}\r\n--sample--\r\nepilogue";
            ContentCodecRegistry registry = new();
            MultipartContentCodec codec = new();
            ContentCodecContext context = new() { Codecs = registry };
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=sample");

            MultipartDocument? document = codec.Deserialize<MultipartDocument>(Encoding.UTF8.GetBytes(body),
                                                                                contentType,
                                                                                context);

            Assert.IsNotNull(document);
            Assert.AreEqual(2, document.Parts.Count);
            Assert.AreEqual("value", document.Parts[0].Name);
            Assert.AreEqual("value", document.Parts[1].Name);
            Assert.AreEqual("first", document.Parts[0].Deserialize<string>());
            Dictionary<string, string>? decoded = document.Parts[1].Deserialize<Dictionary<string, string>>();
            Assert.IsNotNull(decoded);
            Assert.AreEqual("second", decoded["name"]);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("preamble"), document.Preamble);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("epilogue"), document.Epilogue);
        }

        /// <summary>Verifies nested multipart parsing and multipart/related root selection.</summary>
        [TestMethod]
        public void MultipartCodecParsesNestedRelatedContent()
        {
            const string body = "--outer\r\nContent-ID: <metadata>\r\nContent-Type: application/json\r\n\r\n{}\r\n--outer\r\nContent-ID: <nested>\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n--inner\r\nContent-Type: text/plain\r\n\r\nchild\r\n--inner--\r\n\r\n--outer--\r\n";
            ContentCodecRegistry registry = new();
            ContentCodecContext context = new() { Codecs = registry };
            MultipartContentCodec codec = new();
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("multipart/related; boundary=outer; start=\"<nested>\"");

            MultipartDocument? document = codec.Deserialize<MultipartDocument>(Encoding.UTF8.GetBytes(body),
                                                                                contentType,
                                                                                context);

            Assert.IsNotNull(document);
            Assert.AreEqual("nested", document.RootPart?.ContentId);
            Assert.IsNotNull(document.RootPart?.NestedContent);
            Assert.AreEqual("child", document.RootPart.NestedContent.Parts[0].Deserialize<string>());
        }

        /// <summary>Verifies that malformed multipart bodies fail deterministically.</summary>
        [TestMethod]
        public void MultipartCodecRejectsMissingClosingBoundary()
        {
            const string body = "--broken\r\nContent-Type: text/plain\r\n\r\ncontent";
            MultipartContentCodec codec = new();
            ContentCodecRegistry registry = new();
            ContentCodecContext context = new() { Codecs = registry };
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=broken");
            bool exceptionThrown = false;

            try
            {
                codec.Deserialize<MultipartDocument>(Encoding.UTF8.GetBytes(body), contentType, context);
            }
            catch (InvalidDataException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        /// <summary>Verifies typed range metadata for multipart/byteranges.</summary>
        [TestMethod]
        public void MultipartCodecParsesByteRanges()
        {
            const string body = "--range\r\nContent-Type: application/octet-stream\r\nContent-Range: bytes 0-2/10\r\n\r\nabc\r\n--range--\r\n";
            MultipartContentCodec codec = new();
            ContentCodecRegistry registry = new();
            ContentCodecContext context = new() { Codecs = registry };
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("multipart/byteranges; boundary=range");

            MultipartDocument? document = codec.Deserialize<MultipartDocument>(Encoding.ASCII.GetBytes(body),
                                                                                contentType,
                                                                                context);

            Assert.IsNotNull(document);
            Assert.AreEqual(0L, document.Parts[0].ContentRange?.From);
            Assert.AreEqual(2L, document.Parts[0].ContentRange?.To);
            Assert.AreEqual(10L, document.Parts[0].ContentRange?.Length);
        }

        /// <summary>Verifies multipart request creation and codec-backed object parts.</summary>
        [TestMethod]
        public async Task ClientSendsMultipartRequest()
        {
            string? capturedBody = null;
            string? capturedType = null;
            RecordingMessageHandler handler = new(async (request, cancellationToken) =>
            {
                capturedType = request.Content?.Headers.ContentType?.MediaType;
                capturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new ByteArrayContent([]) };
            });
            HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            RestlingClient client = new(context);
            MultipartRequest request = new("https://example.test/upload", RestlingHttpMethod.Post);
            request.AddText("description", "sample")
                   .AddObject("metadata", new Dictionary<string, string> { ["kind"] = "document" }, "application/json")
                   .AddBytes("file", [1, 2, 3], "sample.bin");

            await client.ExecuteMultipartRequestAsync(request);

            Assert.AreEqual("multipart/form-data", capturedType);
            StringAssert.Contains(capturedBody, "description");
            StringAssert.Contains(capturedBody, "metadata");
            StringAssert.Contains(capturedBody, "sample.bin");
            context.Dispose();
        }

        /// <summary>Verifies that x-mixed-replace yields complete parts incrementally.</summary>
        [TestMethod]
        public async Task ClientStreamsMixedReplaceParts()
        {
            const string body = "--frame\r\nContent-Type: text/plain\r\n\r\none\r\n--frame\r\nContent-Type: text/plain\r\n\r\ntwo\r\n--frame--\r\n";
            RecordingMessageHandler handler = new((_, _) =>
            {
                StreamContent content = new(new MemoryStream(Encoding.UTF8.GetBytes(body)));
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/x-mixed-replace; boundary=frame");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            });
            HttpClientContext context = new HttpClientContextBuilder().AddHandler(handler).Build();
            RestlingClient client = new(context);
            RestRequest request = new("https://example.test/events", RestlingHttpMethod.Get);
            List<string?> values = [];

            await foreach (MultipartPart part in client.StreamMultipartMixedReplaceAsync(request))
                values.Add(part.Deserialize<string>());

            CollectionAssert.AreEqual(new[] { "one", "two" }, values);
            context.Dispose();
        }

        #endregion
    }
}
