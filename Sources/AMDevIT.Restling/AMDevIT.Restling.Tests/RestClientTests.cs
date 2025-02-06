using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Core.Network.Builders.Security.Headers;
using AMDevIT.Restling.Tests.Diagnostics;
using AMDevIT.Restling.Tests.Models;
using AMDevIT.Restling.Tests.Models.Authentication;
using AMDevIT.Restling.Tests.Models.Xml;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection.Metadata;

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

        #region Quick methods

        [TestMethod]
        [DataRow("https://httpbin.org/get", "1234")]
        public async Task TestGetAsync(string uri, string parameter)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;

            RestRequestResult<STHttpBinResponse> stRestRequestResult;
            STHttpBinResponse? stsHttpBinResponse;

            uri = $"{uri}?test={parameter}";

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

        [TestMethod]
        [DataRow("https://httpbin.org/xml")]
        public async Task TestGetXMLAsync(string uri)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<Slideshow> restRequestResult;
            Slideshow slideshow;

            restRequestResult = await restlingClient.GetAsync<Slideshow>(uri, cancellationToken);

            Assert.IsNotNull(restRequestResult.Data, "Rest response data for Newtonsoft is null");
            Trace.WriteLine($"Request result: {restRequestResult.Content}");

            slideshow = restRequestResult.Data;
            Trace.WriteLine("Slideshow response:");
            Trace.WriteLine(slideshow.ToString());
        }

        [TestMethod]
        [DataRow("https://httpbin.org/post", "Test name", "Test surname")]
        public async Task TestPostAsync(string uri, string name, string surname)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            HttpDataTestModel testModel = new(name, surname);

            nsRestRequestResult = await restlingClient.PostAsync<NSHttpBinResponse, HttpDataTestModel>(uri, testModel, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }

        [TestMethod]
        [DataRow("https://httpbin.org/put", "Test name", "Test surname")]
        public async Task TestPutAsync(string uri, string name, string surname)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            HttpDataTestModel testModel = new(name, surname);

            nsRestRequestResult = await restlingClient.PutAsync<NSHttpBinResponse, HttpDataTestModel>(uri, testModel, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }

        [TestMethod]
        [DataRow("https://httpbin.org/delete", "1234")]
        public async Task TestDeleteAsync(string uri, string parameter)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;

            RestRequestResult<STHttpBinResponse> stRestRequestResult;
            STHttpBinResponse? stsHttpBinResponse;

            uri = $"{uri}?test={parameter}";

            nsRestRequestResult = await restlingClient.DeleteAsync<NSHttpBinResponse>(uri, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());

            stRestRequestResult = await restlingClient.DeleteAsync<STHttpBinResponse>(uri, cancellationToken);

            Assert.IsNotNull(stRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            stsHttpBinResponse = stRestRequestResult.Data;
            Trace.WriteLine("St response:");
            Trace.WriteLine(stsHttpBinResponse.ToString());
        }

        #endregion

        #region Advanced methods

        [TestMethod]
        [DataRow("https://httpbin.org/get", "1234", "AuthenticationTokeWasHere", "app-version", "1.0.a.b")]
        public async Task TestGetAdvancedAsync(string uri, 
                                               string parameter, 
                                               string authenticationParameter, 
                                               string additionalHeaderKey, 
                                               string additionalHeaderValue)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            AuthenticationHeader authenticationHeader = new ("Bearer", authenticationParameter);
            RequestHeaders requestHeaders = new (authenticationHeader);

            requestHeaders.Headers.Add(additionalHeaderKey, additionalHeaderValue);

            uri = $"{uri}?test={parameter}";

            nsRestRequestResult = await restlingClient.GetAsync<NSHttpBinResponse>(uri, requestHeaders, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }

        [TestMethod]
        [DataRow("https://httpbin.org/post", "Test name", "Test surname", "AuthenticationTokeWasHere", "app-version", "1.0.a.b")]
        public async Task TestPostAdvancedAsync(string uri,
                                                string name, 
                                                string surname,
                                                string authenticationParameter,
                                                string additionalHeaderKey,
                                                string additionalHeaderValue)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            AuthenticationHeader authenticationHeader = new ("Bearer", authenticationParameter);
            RequestHeaders requestHeaders = new (authenticationHeader);
            HttpDataTestModel testModel = new(name, surname);

            requestHeaders.Headers.Add(additionalHeaderKey, additionalHeaderValue);            

            nsRestRequestResult = await restlingClient.PostAsync<NSHttpBinResponse, HttpDataTestModel>(uri, testModel, requestHeaders, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }

        [TestMethod]
        [DataRow("https://httpbin.org/put", "Test name", "Test surname", "AuthenticationTokeWasHere", "app-version", "1.0.a.b")]
        public async Task TestPutAdvancedAsync(string uri,
                                               string name,
                                               string surname,
                                               string authenticationParameter,
                                               string additionalHeaderKey,
                                               string additionalHeaderValue)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            AuthenticationHeader authenticationHeader = new("Bearer", authenticationParameter);
            RequestHeaders requestHeaders = new(authenticationHeader);
            HttpDataTestModel testModel = new(name, surname);

            requestHeaders.Headers.Add(additionalHeaderKey, additionalHeaderValue);

            nsRestRequestResult = await restlingClient.PutAsync<NSHttpBinResponse, HttpDataTestModel>(uri, testModel, requestHeaders, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }

        [TestMethod]
        [DataRow("https://httpbin.org/delete", "1234", "AuthenticationTokeWasHere", "app-version", "1.0.a.b")]
        public async Task TestDeleteAdvancedAsync(string uri,
                                                  string parameter,
                                                  string authenticationParameter,
                                                  string additionalHeaderKey,
                                                  string additionalHeaderValue)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<NSHttpBinResponse> nsRestRequestResult;
            NSHttpBinResponse? nsHttpBinResponse;
            AuthenticationHeader authenticationHeader = new("Bearer", authenticationParameter);
            RequestHeaders requestHeaders = new(authenticationHeader);

            requestHeaders.Headers.Add(additionalHeaderKey, additionalHeaderValue);

            uri = $"{uri}?test={parameter}";

            nsRestRequestResult = await restlingClient.DeleteAsync<NSHttpBinResponse>(uri, requestHeaders, cancellationToken);

            Assert.IsNotNull(nsRestRequestResult.Data, "Rest response data for Newtonsoft is null");

            nsHttpBinResponse = nsRestRequestResult.Data;
            Trace.WriteLine("NS response:");
            Trace.WriteLine(nsHttpBinResponse.ToString());
            Trace.WriteLine($"Raw content: {nsRestRequestResult.Content}");
        }


        

        [TestMethod]
        [DataRow("https://httpbin.org/get", "1234")]
        public async Task TestDeleteErrorHandling(string uri, string parameter)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult restRequestResult;

            uri = $"{uri}?test={parameter}";

            restRequestResult = await restlingClient.DeleteAsync(uri, cancellationToken);
            Assert.IsFalse(restRequestResult.IsSuccessful, "Request result is succesful, but expected not succesful");
            Trace.WriteLine(restRequestResult.ToString());
        }

        [TestMethod]
        [DataRow("https://dummyimage.com/600x400/eee/aaa&text=Example", "C:\\Temp\\test.png")]
        public async Task TestGetDownloadImage(string uri, string destinationPath)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            RestlingClient restlingClient = new(this.Logger);
            RestRequestResult<byte[]> restRequestResult;
            restRequestResult = await restlingClient.GetAsync<byte[]>(uri, cancellationToken);
            Assert.IsTrue(restRequestResult.IsSuccessful, "Request result is not succesful");
            Assert.IsNotNull(restRequestResult.Data, "Rest response data is null");
            Trace.WriteLine(restRequestResult.ToString());            
            await File.WriteAllBytesAsync(destinationPath, restRequestResult.Data, cancellationToken);
        }

        #endregion

        #region Authentications

        [TestMethod]
        [DataRow("https://httpbin.org/basic-auth", "user", "passwd")]
        public async Task TestBasicAuthentication(string uri, string user, string password)
        {
            CancellationToken cancellationToken = this.TestContext.CancellationTokenSource.Token;
            HttpClientContextBuilder httpClientContextBuilder = new();
            RestlingClient restlingClient;
            RestRequestResult<BasicAuthResult> restRequestResult;
            string requestUri = $"{uri}/{user}/{password}";
            AuthenticationHeader authenticationHeader;
            BasicAuthenticationBuilder basicAuthenticationBuilder = new();

            basicAuthenticationBuilder.SetUser(user);
            basicAuthenticationBuilder.SetPassword(password);
            authenticationHeader = basicAuthenticationBuilder.Build();

            httpClientContextBuilder.AddAuthenticationHeader(authenticationHeader);

            restlingClient = new (httpClientContextBuilder, this.logger);
            restRequestResult = await restlingClient.GetAsync<BasicAuthResult>(requestUri, cancellationToken);
            Assert.IsTrue(restRequestResult.IsSuccessful, $"Request result is not succesful. Result: {restRequestResult}");
            Assert.IsNotNull(restRequestResult.Data, $"Rest response data is null. Result: {restRequestResult}");
            Trace.WriteLine(restRequestResult.ToString());
        }

        #endregion

        #endregion
    }
}
