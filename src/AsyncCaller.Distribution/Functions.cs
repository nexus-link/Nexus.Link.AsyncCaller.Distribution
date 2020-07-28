using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AsyncCaller.Distribution
{
    /// <summary>
    /// Defines queue listeners
    /// </summary>
    public static class Functions
    {
        [FunctionName("AC-standard")]
        public static async Task Standard([QueueTrigger("async-caller-standard-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority01")]
        public static async Task Priority01([QueueTrigger("async-caller-priority01-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority02")]
        public static async Task Priority02([QueueTrigger("async-caller-priority02-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority03")]
        public static async Task Priority03([QueueTrigger("async-caller-priority03-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority04")]
        public static async Task Priority04([QueueTrigger("async-caller-priority04-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority05")]
        public static async Task Priority05([QueueTrigger("async-caller-priority05-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority06")]
        public static async Task Priority06([QueueTrigger("async-caller-priority06-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority07")]
        public static async Task Priority07([QueueTrigger("async-caller-priority07-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority08")]
        public static async Task Priority08([QueueTrigger("async-caller-priority08-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority09")]
        public static async Task Priority09([QueueTrigger("async-caller-priority09-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }

        [FunctionName("AC-priority10")]
        public static async Task Priority10([QueueTrigger("async-caller-priority10-queue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(queueItem, log, context);
        }
    }
}
