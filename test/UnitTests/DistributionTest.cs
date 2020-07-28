using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncCaller.Distribution;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nexus.Link.AsyncCaller.Dispatcher.Helpers;
using Nexus.Link.AsyncCaller.Dispatcher.Models;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Core.Platform.Configurations;
using Nexus.Link.Libraries.Web.RestClientHelper;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using RequestEnvelope = Xlent.Lever.AsyncCaller.Data.Models.RequestEnvelope;

namespace UnitTests
{
    [TestClass]
    public class DistributionTest
    {

        private ILogger _logger;
        private ExecutionContext _executionContext;
        private static readonly Tenant Tenant = new Tenant("hoo", "ver");

        private Mock<IHttpClient> _httpSenderMock;
        private Mock<ILeverServiceConfiguration> _asyncCallerServiceConfigMock;
        private Mock<ILeverConfiguration> _asyncCallerConfigMock;
        private static HttpResponseMessage _okResponse;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            FulcrumApplicationHelper.UnitTestSetup(nameof(DistributionTest));
            _okResponse = new HttpResponseMessage(HttpStatusCode.OK);
        }

        [TestInitialize]
        public void Initialize()
        {
            _logger = new LoggerFactory().CreateLogger(nameof(DistributionTest));
            _executionContext = new ExecutionContext { FunctionAppDirectory = AppDomain.CurrentDomain.BaseDirectory };
            _httpSenderMock = new Mock<IHttpClient>();
            _httpSenderMock.Setup(x => x.SimulateOutgoingCalls).Returns(true);
            _asyncCallerConfigMock = new Mock<ILeverConfiguration>();
            _asyncCallerServiceConfigMock = new Mock<ILeverServiceConfiguration>();
            _asyncCallerServiceConfigMock.Setup(x => x.GetConfigurationForAsync(Tenant)).ReturnsAsync(_asyncCallerConfigMock.Object);

            _asyncCallerConfigMock.Setup(x => x.MandatoryValue<string>("ConnectionString")).Returns(RequestQueueHelper.MemoryQueueConnectionString);
            _asyncCallerConfigMock.Setup(x => x.MandatoryValue<string>("QueueName")).Returns("the-queue");

            Distributor.HttpSender = _httpSenderMock.Object;
            Distributor.AsyncCallerServiceConfiguration = _asyncCallerServiceConfigMock.Object;
        }

        private async Task<RequestEnvelope> CreateRequestEnvelopeAsync(HttpMethod method, string body)
        {
            return new RequestEnvelope
            {
                Organization = Tenant.Organization,
                Environment = Tenant.Environment,
                CreatedAt = DateTimeOffset.Now,
                DeadlineAt = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(10)),
                RawRequest = await new Request
                {
                    CallOut = new HttpRequestMessage(method, "http://example.org")
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    }
                }.ToDataAsync()
            };
        }

        [TestMethod]
        public async Task Distributor_Sends_Get_Request()
        {
            const string expectedRequestBody = "{ 'Who': 'Vher' }";
            string actualRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback((HttpRequestMessage request, CancellationToken token) => { actualRequestBody = request.Content.ReadAsStringAsync().Result; })
                .ReturnsAsync(_okResponse)
                .Verifiable();

            var requestEnvelope = await CreateRequestEnvelopeAsync(HttpMethod.Get, expectedRequestBody);
            await Functions.Standard(requestEnvelope, _logger, _executionContext);

            _httpSenderMock.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(expectedRequestBody, actualRequestBody);
        }

        // TODO
        // Anropa Function med request som har callback
        // Response läggs på minneskö, gör intercept för att verifiera
        // Intercept med IHttpSender

    }
}
