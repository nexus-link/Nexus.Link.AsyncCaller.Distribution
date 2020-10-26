using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nexus.Link.AsyncCaller.Sdk.Data.Models;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Helpers;
using Nexus.Link.Libraries.Core.Platform.ServiceMetas;

namespace AsyncCaller.Distribution
{
    /// <summary>
    /// Defines queue listeners
    /// </summary>
    /// <remarks>
    /// The queue names 
    /// </remarks>
    public static class Functions
    {
        [FunctionName("ReleaseHistory")]
        public static List<Release> Version([HttpTrigger(AuthorizationLevel.Anonymous, "get", "api/v1/ServiceMetas/Releases")] HttpRequest request)
        {
            return ReleaseHistory.Releases;
        }

        [FunctionName("Logging")]
        public static void Logging([QueueTrigger("platform-integration-test-template-service-logging")] string item, ILogger log)
        {
            log.LogInformation(item);
        }

        [FunctionName("AC-standard")]
        public static async Task Standard([QueueTrigger(RequestQueueHelper.DefaultQueueName, Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority01")]
        public static async Task Priority01([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "1", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority02")]
        public static async Task Priority02([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "2", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority03")]
        public static async Task Priority03([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "3", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority04")]
        public static async Task Priority04([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "4", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority05")]
        public static async Task Priority05([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "5", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority06")]
        public static async Task Priority06([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "6", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority07")]
        public static async Task Priority07([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "7", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority08")]
        public static async Task Priority08([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "8", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority09")]
        public static async Task Priority09([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "9", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        [FunctionName("AC-priority10")]
        public static async Task Priority10([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.MultipleQueueNameInterfix + "10", Connection = "AzureWebJobsStorage")] RawRequestEnvelope rawRequestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(rawRequestEnvelope, log);
        }

        // Note! If you need more queues, please contact Nexus Link Support.
    }
}
