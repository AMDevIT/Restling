using AMDevIT.Restling.Core;
using AMDevIT.Restling.Tests.Diagnostics;
using AMDevIT.Restling.Tests.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AMDevIT.Restling.Tests
{
    [TestClass]
    public sealed class RestClientTests
    {
        #region Fields

        private ILogger<RestClientTests> logger = null!;

        #endregion

        #region Properties

        public TestContext TestContext
        {
            get;
            set;
        } = null!;

        private ILogger<RestClientTests> Logger => this.logger;

        #endregion        

        #region Methods

        [TestInitialize]
        public void Initialize()
        {
            // Inizializza il logger prima di ogni test
            this.logger = new TraceLogger<RestClientTests>();
        }
        

        [TestMethod]
        [DataRow("https://httpbin.org/get")]
        public async Task TestGetAsync(string uri)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;

            RestRequestResult<STHttpBinResponse> stRestRequestResult;
            STHttpBinResponse? stsHttpBinResponse;

            nsRestRequestResult = await restlingClient.GetAsync<NSHttpBinResponse>(uri, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());

            stRestRequestResult = await restlingClient.GetAsync<STHttpBinResponse>(uri, cancellationToken);

            Assert.IsNotNull(stRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            stsHttpBinResponse = stRestRequestResult.Data;
            Trace.WriteLine("St response:");
            Trace.WriteLine(stsHttpBinResponse.ToString());
        }

        #endregion
    }
}
