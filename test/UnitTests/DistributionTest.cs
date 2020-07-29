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
using Newtonsoft.Json;
using Nexus.Link.AsyncCaller.Dispatcher.Helpers;
using Nexus.Link.AsyncCaller.Dispatcher.Models;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.Logging;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Core.Platform.Configurations;
using Nexus.Link.Libraries.Core.Threads;
using Nexus.Link.Libraries.Web.RestClientHelper;
using Xlent.Lever.AsyncCaller.Storage.Memory.Queue;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using RequestEnvelope = Xlent.Lever.AsyncCaller.Data.Models.RequestEnvelope;

namespace UnitTests
{
    [TestClass]
    public class DistributionTest
    {
        #region Initilize

        private static ILogger _logger;
        private static ExecutionContext _executionContext;
        private static readonly Tenant Tenant = new Tenant("hoo", "ver");
        private const string QueueName = "the-queue";
        private static bool _runBackgroundJob;

        private Mock<IHttpClient> _httpSenderMock;
        private Mock<ILeverServiceConfiguration> _asyncCallerServiceConfigMock;
        private Mock<ILeverConfiguration> _asyncCallerConfigMock;
        private static HttpResponseMessage _okResponse;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            FulcrumApplicationHelper.UnitTestSetup(nameof(DistributionTest));
            _logger = new LoggerFactory().CreateLogger(nameof(DistributionTest));
            _executionContext = new ExecutionContext { FunctionAppDirectory = AppDomain.CurrentDomain.BaseDirectory };
            _runBackgroundJob = true;
            _okResponse = new HttpResponseMessage(HttpStatusCode.OK);

            SimulateQueueTrigger(null);
            SimulateQueueTrigger(1);
            SimulateQueueTrigger(2);
        }


        [ClassCleanup]
        public static void ClassCleanup()
        {
            _runBackgroundJob = false;
        }

        [TestInitialize]
        public void Initialize()
        {
            _httpSenderMock = new Mock<IHttpClient>();
            _httpSenderMock.Setup(x => x.SimulateOutgoingCalls).Returns(true);
            _asyncCallerConfigMock = new Mock<ILeverConfiguration>();
            _asyncCallerServiceConfigMock = new Mock<ILeverServiceConfiguration>();
            _asyncCallerServiceConfigMock.Setup(x => x.GetConfigurationForAsync(Tenant)).ReturnsAsync(_asyncCallerConfigMock.Object);

            // By using RequestQueueHelper.MemoryQueueConnectionString, the SDK uses MemoryQueue.Instance(QueueName) as the queue
            _asyncCallerConfigMock.Setup(x => x.MandatoryValue<string>("ConnectionString")).Returns(RequestQueueHelper.MemoryQueueConnectionString);
            _asyncCallerConfigMock.Setup(x => x.Value<string>("DistributionVersion")).Returns("2");
            _asyncCallerConfigMock.Setup(x => x.Value<double?>("DefaultDeadlineTimeSpanInSeconds")).Returns(60);

            Distributor.HttpSender = _httpSenderMock.Object;
            Distributor.AsyncCallerServiceConfiguration = _asyncCallerServiceConfigMock.Object;
        }

        #endregion

        #region Helpers

        private static MemoryQueue GetQueue(int? priority)
        {
            return MemoryQueue.Instance(RequestQueueHelper.GetQueueNameForDistributionVersion2(priority));
        }

        /// <summary>
        /// Listens for messages on <see cref="MemoryQueue"/> and runs them through <see cref="Functions.Standard"/>, simulating a QueueTrigger.
        /// </summary>
        private static void SimulateQueueTrigger(int? priority)
        {
            var queue = GetQueue(priority);
            ThreadHelper.FireAndForget(async () =>
            {
                while (_runBackgroundJob)
                {
                    var message = queue.GetOneMessageNoBlock();
                    if (message == null) continue;
                    var requestEnvelope = JsonConvert.DeserializeObject<RequestEnvelopeMock>(message);
                    Log.LogInformation($"Message on queue '{queue.QueueName}: {requestEnvelope.RawRequest.Id}");
                    await Functions.Standard(requestEnvelope, _logger, _executionContext);
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            });
        }

        private static async Task<RequestEnvelope> CreateRequestEnvelopeAsync(HttpMethod method, string body, int? priority = null)
        {
            var envelope = new RequestEnvelopeMock
            {
                Organization = Tenant.Organization,
                Environment = Tenant.Environment,
                CreatedAt = DateTimeOffset.Now,
                DeadlineAt = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(10)),
                RawRequest = await new Request
                {
                    Id = Guid.NewGuid().ToString(),
                    CallOut = new HttpRequestMessage(method, "http://example.org")
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    }
                }.ToDataAsync()
            };
            envelope.RawRequest.Priority = priority;
            return envelope;
        }

        private static HttpResponseMessage BuildResponse(HttpStatusCode failCode)
        {
            return new HttpResponseMessage(failCode);
        }

        #endregion

        [DataRow("GET", null)]
        [DataRow("GET", 1)]
        [DataRow("GET", 2)]
        [DataRow("POST", null)]
        [DataRow("POST", 1)]
        [DataRow("PUT", null)]
        [DataRow("PATCH", null)]
        [TestMethod]
        public async Task Distributor_Sends_Request(string method, int? priority)
        {
            const string expectedRequestBody = "{ 'foo': 'bar' }";
            string actualRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) => { actualRequestBody = await request.Content.ReadAsStringAsync(); })
                .ReturnsAsync(_okResponse)
                .Verifiable();

            var requestEnvelope = await CreateRequestEnvelopeAsync(new HttpMethod(method), expectedRequestBody, priority);
            await Functions.Standard(requestEnvelope, _logger, _executionContext);

            _httpSenderMock.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(expectedRequestBody, actualRequestBody);
        }

        /// <summary>
        /// This test will make sure that a request that got a failure response is handled in the correct way.
        ///
        /// We mock the first response to be failure. If it should be retried, the second response is mock to be ok.
        ///
        /// We rely on <see cref="SimulateQueueTrigger"/> to listen to the queue and sending them again with <see cref="Functions.Standard"/>.
        /// </summary>
        /// <remarks>
        /// When a message is put back on queue, there is a hard coded 1 second delay,
        /// which means that test runs with "shouldBeRetried" true will take at least one second.
        /// </remarks>
        [DataRow(HttpStatusCode.BadRequest, false, null)]
        [DataRow(HttpStatusCode.NotFound, true, null)]
        [DataRow(HttpStatusCode.NotFound, true, 1)]
        [DataRow(HttpStatusCode.NotFound, true, 2)]
        [DataRow(HttpStatusCode.RequestTimeout, true, null)]
        [DataRow((HttpStatusCode)425, true, null)]
        [DataRow((HttpStatusCode)429, true, null)]
        [DataRow(HttpStatusCode.InternalServerError, true, null)]
        [DataRow(HttpStatusCode.InternalServerError, true, 1)]
        [DataRow(HttpStatusCode.BadGateway, true, null)]
        [DataRow((HttpStatusCode)510, false, null)]
        [DataRow((HttpStatusCode)526, false, null)]
        [TestMethod]
        public async Task Distributor_Handles_Retries(HttpStatusCode failCode, bool shouldBeRetried, int? priority)
        {
            await GetQueue(priority).ClearAsync();

            var firstCall = new ManualResetEvent(false);
            var secondCall = new ManualResetEvent(false);
            const string expectedRequestBody = "{ 'foo': 'bar' }";

            var count = 0;
            string actualRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    if (count == 0) firstCall.Set();
                    else secondCall.Set();
                    count++;
                    actualRequestBody = await request.Content.ReadAsStringAsync();
                })
                .ReturnsAsync(count == 0 ? BuildResponse(failCode) : _okResponse);

            var requestEnvelope = await CreateRequestEnvelopeAsync(HttpMethod.Get, expectedRequestBody, priority);
            await Functions.Standard(requestEnvelope, _logger, _executionContext);

            Assert.IsTrue(firstCall.WaitOne(TimeSpan.FromSeconds(2)));

            if (shouldBeRetried)
            {
                Assert.IsTrue(secondCall.WaitOne(TimeSpan.FromSeconds(2)));
                Assert.AreEqual(2, count, "The http sender should have been call twice");
            }
            else
            {
                Assert.AreEqual(1, count, "The http sender should have been call once");
            }

            Assert.AreEqual(expectedRequestBody, actualRequestBody, "The request body should survive being re-queued");
        }
    }

    // TODO: Callback
}
