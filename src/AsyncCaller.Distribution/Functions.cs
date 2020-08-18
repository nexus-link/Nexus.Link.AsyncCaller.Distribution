using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Nexus.Link.AsyncCaller.Dispatcher.Helpers;
using Xlent.Lever.AsyncCaller.Data.Models;

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
        [FunctionName("AC-standard")]
        public static async Task Standard([QueueTrigger(RequestQueueHelper.DefaultQueueName, Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority01")]
        public static async Task Priority01([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "1", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority02")]
        public static async Task Priority02([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "2", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority03")]
        public static async Task Priority03([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "3", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority04")]
        public static async Task Priority04([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "4", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority05")]
        public static async Task Priority05([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "5", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority06")]
        public static async Task Priority06([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "6", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority07")]
        public static async Task Priority07([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "7", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority08")]
        public static async Task Priority08([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "8", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority09")]
        public static async Task Priority09([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "9", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        [FunctionName("AC-priority10")]
        public static async Task Priority10([QueueTrigger(RequestQueueHelper.DefaultQueueName + RequestQueueHelper.PriorityQueueNameInterfix + "10", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log)
        {
            await Distributor.DistributeCall(requestEnvelope, log);
        }

        // Note! If you need more queues, please contact Nexus Link Support.
    }
}
