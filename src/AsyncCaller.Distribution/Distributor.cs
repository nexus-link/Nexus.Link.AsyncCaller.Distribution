using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AsyncCaller.Distribution
{
    public static class Distributor
    {
        public static async Task DistributeCall(string queueItem, ILogger log, ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}