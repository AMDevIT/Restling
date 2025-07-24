using AMDevIT.Restling.Core;
using AMDevIT.Restling.Tests.Diagnostics;
using AMDevIT.Restling.Tests.Models.Security;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public class SecurityTests
    {
        #region Fields

        private ILogger<SecurityTests> logger = null!;

        #endregion

        #region Properties

        public TestContext TestContext
        {
            get;
            set;
        } = null!;

        private ILogger Logger => this.logger;

        #endregion

        #region Methods

        [TestInitialize]
        public void Initialize()
        {
            // Inizializza il logger prima di ogni test
            this.logger = new TraceLogger<SecurityTests>();
        }

        [TestMethod]
        [DynamicData(nameof(GenerateXMLXEEData), DynamicDataSourceType.Method)]
        public Task TestXMLXEEExecutionAsync(string xml, bool allowUnsafe, bool containsUnsafe)
        {
#if DEBUG
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;

            HttpResponseMessage response = new (System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };

            if (response.Content.Headers.ContentType == null)
                response.Content.Headers.ContentType = new ("application/xml");

            HttpResponseParser httpResponseParser = new(this.Logger)
            {
                AllowUnsafeXml = allowUnsafe
            };

            RestRequest restRequest = new("http://localhost/test", Core.HttpMethod.Get);
            TimeSpan elapsed = TimeSpan.FromMilliseconds(1000);
            RestRequestResult<SecurityXMLTestPayload> result = await httpResponseParser.DecodeAsync<SecurityXMLTestPayload>(response,
                                                                                        restRequest,
                                                                                        elapsed,
                                                                                        cancellationToken: cancellationToken);
            if (allowUnsafe == false)
            {
                if (containsUnsafe)
                {
                    Assert.IsNotNull(result.Exception, "Exception should not be null. Unsafe XML code is not allowed.");
                    Assert.IsTrue(result.Exception.InnerException?.Message.Contains("DtdProcessing"),
                                  $"The exception is not related to Dtd Processing. Error: {result.Exception}");
                    this.Logger.LogInformation(result.Exception, "Unsafe XML code detected correctly and blocked.");
                }
                else
                    Assert.IsNull(result.Exception, "Exception should be null. Unsafe XML code is not allowed but not present.");
            }
            else
            {
                Assert.IsNull(result.Exception, "Exception should be null. Unsafe XML code is allowed.");
                Assert.IsNotNull(result.Data, "Data must be not null. Unsafe XML code is allowed.");
                this.Logger.LogInformation("Unsafe XML code detected correctly and data parsed.");
            }
#else
            this.Logger.LogError("This test requires DEBUG configuration to run.");
#endif 
            return Task.CompletedTask;
        }

        private static IEnumerable<object[]> GenerateXMLXEEData()
        {
            List<object[]> data = [];

            string winHostXML = 
                @"<?xml version=""1.0"" ?>
                    <!DOCTYPE foo [
                      <!ELEMENT foo ANY >
                      <!ENTITY xxe SYSTEM ""file:///C:/Windows/System32/drivers/etc/hosts"" >]>
                    <foo>&xxe;</foo>";
            string winIniXML =
                @"<?xml version=""1.0"" ?>
                    <!DOCTYPE foo [
                      <!ELEMENT foo ANY >
                      <!ENTITY xxe SYSTEM ""file:///C:/Windows/win.ini"" >]>
                    <foo>&xxe;</foo>";
            string cleanXml = 
                @"<?xml version=""1.0"" ?>
                    <foo>Test</foo>";

            data.Add([winHostXML, false, true]);
            data.Add([winIniXML, false, true]);
            data.Add([cleanXml, false, false]);
            data.Add([winHostXML, true, true]);
            data.Add([winIniXML, true, true]);
            data.Add([cleanXml, true, false]);
            return data;
        }

#endregion
    }
}
