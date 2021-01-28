using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncCaller.Distribution;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nexus.Link.AsyncCaller.Sdk.Data.Models;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Helpers;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Models;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.Decoupling;
using Nexus.Link.Libraries.Core.Logging;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Core.Platform.Configurations;
using Nexus.Link.Libraries.Web.RestClientHelper;

namespace UnitTests
{
    [TestClass]
    public class LogTests
    {
        private static readonly Tenant Tenant = new Tenant("hoo", "ver");

        [TestInitialize]
        public void InitializeTest()
        {
            FulcrumApplicationHelper.UnitTestSetup(nameof(LogTests));

            var httpSenderMock = new Mock<IHttpClient>();
            httpSenderMock.Setup(x => x.SimulateOutgoingCalls).Returns(true);
            Distributor.HttpSender = httpSenderMock.Object;

            var asyncCallerConfigMock = new Mock<ILeverConfiguration>();
            var asyncCallerServiceConfigMock = new Mock<ILeverServiceConfiguration>();
            asyncCallerServiceConfigMock.Setup(x => x.GetConfigurationForAsync(Tenant)).ReturnsAsync(asyncCallerConfigMock.Object);

            // By using RequestQueueHelper.MemoryQueueConnectionString, the SDK uses MemoryQueue.Instance(QueueName) as the queue
            asyncCallerConfigMock.Setup(x => x.MandatoryValue<string>("ConnectionString")).Returns(RequestQueueHelper.MemoryQueueConnectionString);
            asyncCallerConfigMock.Setup(x => x.Value<int?>(nameof(AnonymousSchema.SchemaVersion))).Returns(1);
            asyncCallerConfigMock.Setup(x => x.Value<double?>("DefaultDeadlineTimeSpanInSeconds")).Returns(60);

            var serviceConfigs = new Dictionary<Tenant, ILeverServiceConfiguration>
            {
                [Tenant] = asyncCallerServiceConfigMock.Object,
            };
            Startup.AsyncCallerServiceConfiguration = serviceConfigs;
        }

        [TestMethod]
        public async Task Distribution_Invocation_Logged_Verbose()
        {
            var logger = new LoggerFactory().CreateLogger(nameof(LogTests));
            var mockLogger = new Mock<ISyncLogger>();
            FulcrumApplication.Setup.SynchronousFastLogger = mockLogger.Object;

            var testDone = new ManualResetEvent(false);
            var origionalId = Guid.NewGuid().ToString();
            var url = $"https://example.org/api/v1/stuffs/{origionalId}";

            mockLogger
                .Setup(x => x.LogSync(It.IsAny<LogRecord>()))
                .Callback((LogRecord record) =>
                {
                    Console.WriteLine(record.Message);
                    if (record.SeverityLevel == LogSeverityLevel.Verbose &&
                        record.Message.Contains(origionalId) &&
                        record.Message.Contains(url) &&
                        record.ToLogString().Contains("corr-id"))
                    {
                        testDone.Set();
                    }
                });

            var callOut = new HttpRequestMessage(HttpMethod.Get, url);
            callOut.Headers.Add("X-Correlation-ID", "corr-id");

            var requestEnvelope = new RawRequestEnvelope
            {
                Organization = Tenant.Organization,
                Environment = Tenant.Environment,
                OriginalRequestId = origionalId,
                RawRequest = await new Request { CallOut = callOut }.ToRawAsync()

            };
            await Distributor.DistributeCall(requestEnvelope, logger);

            Assert.IsTrue(testDone.WaitOne(1000));
        }
    }
}
