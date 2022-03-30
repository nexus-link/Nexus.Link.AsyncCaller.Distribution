using System;
using System.Collections.Generic;
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
using Nexus.Link.AsyncCaller.Sdk.Common.Models;
using Nexus.Link.AsyncCaller.Sdk.Data.Models;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Helpers;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Models;
using Nexus.Link.AsyncCaller.Sdk.Storage.Memory.Queue;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.Decoupling;
using Nexus.Link.Libraries.Core.Logging;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Core.Platform.Configurations;
using Nexus.Link.Libraries.Core.Threads;
using Nexus.Link.Libraries.Web.RestClientHelper;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 1)]

namespace UnitTests
{
    [TestClass]
    public class DistributionTest
    {
        #region Initilize

        private static ILogger _logger;
        private const string Organization = "hoo";
        private const string Enviorment = "ver";
        private const string Enviorment2 = "local-dev";
        private static readonly Tenant Tenant = new Tenant(Organization, Enviorment);
        private static readonly Tenant Tenant2 = new Tenant(Organization, Enviorment2);
        private static bool _runBackgroundJob;

        private Mock<IHttpClient> _httpSenderMock;
        private static Mock<ILeverServiceConfiguration> _asyncCallerServiceConfigMock;
        private static Mock<ILeverConfiguration> _asyncCallerConfigMock;
        private static HttpResponseMessage _okResponse;
        private const string OkContent = "{ 'Status': 'OK' }";

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            FulcrumApplicationHelper.UnitTestSetup(nameof(DistributionTest));
            FulcrumApplication.Setup.SynchronousFastLogger = new MinimalFulcrumLogger();
            _logger = new LoggerFactory().CreateLogger(nameof(DistributionTest));
            _okResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(OkContent, Encoding.UTF8, "application/json")
            };
            _runBackgroundJob = true;

            _asyncCallerConfigMock = new Mock<ILeverConfiguration>();
            _asyncCallerServiceConfigMock = new Mock<ILeverServiceConfiguration>();
            _asyncCallerServiceConfigMock.Setup(x => x.GetConfigurationForAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>())).ReturnsAsync(_asyncCallerConfigMock.Object);

            // By using RequestQueueHelper.MemoryQueueConnectionString, the SDK uses MemoryQueue.Instance(QueueName) as the queue
            _asyncCallerConfigMock.Setup(x => x.MandatoryValue<string>("ConnectionString")).Returns(RequestQueueHelper.MemoryQueueConnectionString);
            // This project is based on SchemaVersion 1
            _asyncCallerConfigMock.Setup(x => x.Value<int?>(nameof(AnonymousSchema.SchemaVersion))).Returns(1);
            _asyncCallerConfigMock.Setup(x => x.Value<double?>("DefaultDeadlineTimeSpanInSeconds")).Returns(60);

            var serviceConfigs = new Dictionary<Tenant, ILeverServiceConfiguration>
            {
                [Tenant] = _asyncCallerServiceConfigMock.Object,
                [Tenant2] = _asyncCallerServiceConfigMock.Object,
            };
            Startup.AsyncCallerServiceConfiguration = serviceConfigs;

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
        public async Task Initialize()
        {
            Log.LogInformation("Initializing (new http sender mock, etc)");
            _httpSenderMock = new Mock<IHttpClient>();
            _httpSenderMock.Setup(x => x.SimulateOutgoingCalls).Returns(true);

            Distributor.HttpSender = _httpSenderMock.Object;

            await GetQueue(null).ClearAsync();
            await GetQueue(1).ClearAsync();
            await GetQueue(2).ClearAsync();
        }

        #endregion

        #region Helpers

        private static MemoryQueue GetQueue(int? priority)
        {
            return (MemoryQueue)RequestQueueHelper.GetRequestQueueOrThrow(Tenant, _asyncCallerConfigMock.Object, priority).GetQueue();
        }

        /// <summary>
        /// Listens for messages on <see cref="MemoryQueue"/> and runs them through <see cref="Distributor"/>, simulating a QueueTrigger.
        /// </summary>
        private static void SimulateQueueTrigger(int? priority)
        {
            Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
            ThreadHelper.FireAndForget(async () =>
            {
                var queue = GetQueue(priority);
                Log.LogInformation($"Listening to queue '{queue}'");
                while (_runBackgroundJob)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    Log.LogVerbose($"Checking for message on queue '{queue}'");
                    var message = queue.GetOneMessageNoBlock();
                    if (message == null) continue;
                    var requestEnvelope = JsonConvert.DeserializeObject<RequestEnvelopeMock>(message);
                    Log.LogInformation($"Popped '{queue.QueueName}' | {requestEnvelope.RawRequest.Title} | prio: '{requestEnvelope.RawRequest.Priority}' | attempts: {requestEnvelope.Attempts}");
                    await Distributor.DistributeCall(requestEnvelope, _logger);
                }
                Log.LogInformation($"Stopped listening to queue '{queue}'");
            });
        }

        private static async Task<RawRequestEnvelope> CreateRequestEnvelopeAsync(Tenant tenant, HttpMethod callOutMethod, string body, string path, HttpMethod callBackMethod = null, int? priority = null)
        {
            var url = $"https://example.org{path}";
            var request = new Request
            {
                Id = Guid.NewGuid().ToString(),
                CallOut = new HttpRequestMessage(callOutMethod, url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                }
            };
            if (callBackMethod != null)
            {
                request.CallBack = new HttpRequestMessage(callBackMethod, $"{url}/callback");
            }
            var envelope = new RequestEnvelopeMock
            {
                Organization = tenant.Organization,
                Environment = tenant.Environment,
                CreatedAt = DateTimeOffset.Now,
                DeadlineAt = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(10)),
                RawRequest = await request.ToRawAsync()
            };
            envelope.RawRequest.Priority = priority;
            return envelope;
        }

        private static HttpResponseMessage BuildResponse(HttpStatusCode failCode)
        {
            return new HttpResponseMessage(failCode);
        }

        #endregion

        [DataRow("GET", Enviorment, null)]
        [DataRow("GET", Enviorment, 1)]
        [DataRow("GET", Enviorment, 2)]
        [DataRow("GET", Enviorment2, null)]
        [DataRow("GET", Enviorment2, 1)]
        [DataRow("GET", Enviorment2, 2)]
        [DataRow("POST", Enviorment, null)]
        [DataRow("POST", Enviorment, 1)]
        [DataRow("PUT", Enviorment, null)]
        [DataRow("PATCH", Enviorment, null)]
        [TestMethod]
        public async Task Distributor_Sends_Request(string method, string enviorment, int? priority)
        {
            var tenant = new Tenant(Organization, enviorment);
            const string expectedRequestBody = "{ 'foo': 'bar' }";
            string actualRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) => { actualRequestBody = await request.Content.ReadAsStringAsync(); })
                .ReturnsAsync(_okResponse)
                .Verifiable();

            var requestEnvelope = await CreateRequestEnvelopeAsync(tenant, new HttpMethod(method), expectedRequestBody, "/", null, priority);
            await Distributor.DistributeCall(requestEnvelope, _logger);

            _httpSenderMock.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(expectedRequestBody, actualRequestBody);
        }

        [DataRow(null)]
        [DataRow(1)]
        [DataRow(2)]
        [TestMethod]
        public async Task Distributor_Sends_Request_With_Callback(int? priority)
        {
            const string expectedRequestBody = "{ 'foo': 'bar' }";
            string actualRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.Is((HttpRequestMessage request) => request.Method == HttpMethod.Get), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) => { actualRequestBody = await request.Content.ReadAsStringAsync(); })
                .ReturnsAsync(_okResponse)
                .Verifiable();

            var resetEvent = new ManualResetEvent(false);
            string callbackRequestBody = null;
            _httpSenderMock
                .Setup(x => x.SendAsync(It.Is((HttpRequestMessage request) => request.Method == HttpMethod.Post), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    callbackRequestBody = await request.Content.ReadAsStringAsync();
                    resetEvent.Set();
                })
                .ReturnsAsync(_okResponse)
                .Verifiable();

            var requestEnvelope = await CreateRequestEnvelopeAsync(Tenant, HttpMethod.Get, expectedRequestBody, "/", HttpMethod.Post, priority);
            await Distributor.DistributeCall(requestEnvelope, _logger);

            Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(3)));
            _httpSenderMock.Verify(x => x.SendAsync(It.Is((HttpRequestMessage request) => request.Method == HttpMethod.Get), It.IsAny<CancellationToken>()), Times.Once);
            _httpSenderMock.Verify(x => x.SendAsync(It.Is((HttpRequestMessage request) => request.Method == HttpMethod.Post), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(expectedRequestBody, actualRequestBody);

            var callbackResponseContent = JsonConvert.DeserializeObject<ResponseContent>(callbackRequestBody);
            Assert.AreEqual(HttpStatusCode.OK, callbackResponseContent.StatusCode);
            Assert.AreEqual(OkContent, callbackResponseContent.Payload);
            Assert.AreEqual("application/json", callbackResponseContent.PayloadMediaType);
        }

        /// <summary>
        /// This test will make sure that a request that got a failure response is handled in the correct way.
        ///
        /// We mock the first response to be failure. If it should be retried, the second response is mocked to be ok.
        ///
        /// We rely on <see cref="SimulateQueueTrigger"/> to listen to the queue and sending them again with <see cref="Distributor.DistributeCall"/>.
        /// </summary>
        /// <remarks>
        /// When a message is put back on queue, there is a hard coded 1 second delay,
        /// which means that test runs with "shouldBeRetried" true will take at least one second.
        /// </remarks>
        [DataRow((HttpStatusCode)425, true, null)] // Too Early
        [DataRow(HttpStatusCode.Locked, true, 1)]
        [DataRow(HttpStatusCode.TooManyRequests, true, 2)]
        [DataRow(HttpStatusCode.TooManyRequests, true, null)]
        [DataRow(HttpStatusCode.BadGateway, true, null)]
        [DataRow(HttpStatusCode.BadGateway, true, 1)]
        [DataRow(HttpStatusCode.BadRequest, false, null)]
        [DataRow(HttpStatusCode.InternalServerError, true, null)]
        [DataRow(HttpStatusCode.InternalServerError, true, 1)]
        [DataRow(HttpStatusCode.NotExtended, false, null)]
        [DataRow(HttpStatusCode.NotFound, true, null)]
        [DataRow(HttpStatusCode.NotFound, true, 1)]
        [DataRow(HttpStatusCode.NotFound, true, 2)]
        [DataRow(HttpStatusCode.RequestTimeout, true, null)]
        [DataRow(HttpStatusCode.AlreadyReported, false, null)]
        
        [TestMethod]
        public async Task Distributor_Handles_Retries(HttpStatusCode failCode, bool shouldBeRetried, int? priority)
        {
            var firstCall = new ManualResetEvent(false);
            var secondCall = new ManualResetEvent(false);
            const string expectedRequestBody = "{ 'foo': 'bar' }";

            var path = $"/{failCode}/{shouldBeRetried}/{priority ?? -1}";

            var count = 0;
            var failedResponseReturned = false;
            string actualRequestBody = null;

            _httpSenderMock
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    Log.LogInformation($"HTTP call {request.RequestUri} | count: {count}");
                    Assert.AreEqual(path, request.RequestUri.PathAndQuery);
                    count++;
                    if (count == 1) firstCall.Set();
                    else secondCall.Set();

                    actualRequestBody = await request.Content.ReadAsStringAsync();
                })
                .ReturnsAsync(() =>
                {
                    var response = !failedResponseReturned ? BuildResponse(failCode) : _okResponse;
                    failedResponseReturned = true;
                    return response;
                });
            
            // Fake popping the queue with a AC request envelope
            var requestEnvelope = await CreateRequestEnvelopeAsync(Tenant, HttpMethod.Get, expectedRequestBody, path, null, priority);
            await Distributor.DistributeCall(requestEnvelope, _logger);

            Assert.IsTrue(firstCall.WaitOne(TimeSpan.FromSeconds(5)), "Expected a call to the Http Sender");


            if (shouldBeRetried)
            {
                Assert.IsTrue(secondCall.WaitOne(TimeSpan.FromSeconds(5)), "Expected a second call to the Http Sender");
                Assert.AreEqual(2, count, "The http sender should have been called twice");
            }
            else
            {
                await Task.Delay(100);
                Assert.AreEqual(1, count, "The http sender should have been call once");
            }

            Assert.AreEqual(expectedRequestBody, actualRequestBody, "The request body should survive being re-queued");
        }
    }

    public class MinimalFulcrumLogger : ISyncLogger
    {
        public void LogSync(LogRecord logRecord)
        {
            var message = $"{logRecord.TimeStamp:HH:mm:ss.ffff} | {logRecord.Message}";
            Console.WriteLine(message);
        }
    }
}
