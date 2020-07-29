using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Xlent.Lever.AsyncCaller.Data.Models;
using FulcrumApplicationHelper = Nexus.Link.Libraries.Web.AspNet.Application.FulcrumApplicationHelper;

namespace AsyncCaller.Distribution
{
    /// <summary>
    /// Defines queue listeners
    /// </summary>
    public static class Functions
    {
        static Functions()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var serviceOrganization = config.GetValue<string>("Nexus:Organization");
            var serviceEnvironment = config.GetValue<string>("Nexus:Environment");
            var serviceTenant = new Tenant(serviceOrganization, serviceEnvironment);
            var runtimeLevelString = config.GetValue<string>("Nexus:RunTimeLevel");
            if (!Enum.TryParse(runtimeLevelString, out RunTimeLevelEnum runtimeLevel)) runtimeLevel = RunTimeLevelEnum.Production;
            FulcrumApplicationHelper.WebBasicSetup($"async-caller-function-app-{serviceTenant.Organization}-{serviceTenant.Environment}", serviceTenant, runtimeLevel);
        }

        [FunctionName("AC-standard")]
        public static async Task Standard([QueueTrigger("async-caller-standard-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority01")]
        public static async Task Priority01([QueueTrigger("async-caller-priority01-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority02")]
        public static async Task Priority02([QueueTrigger("async-caller-priority02-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority03")]
        public static async Task Priority03([QueueTrigger("async-caller-priority03-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority04")]
        public static async Task Priority04([QueueTrigger("async-caller-priority04-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority05")]
        public static async Task Priority05([QueueTrigger("async-caller-priority05-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority06")]
        public static async Task Priority06([QueueTrigger("async-caller-priority06-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority07")]
        public static async Task Priority07([QueueTrigger("async-caller-priority07-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority08")]
        public static async Task Priority08([QueueTrigger("async-caller-priority08-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority09")]
        public static async Task Priority09([QueueTrigger("async-caller-priority09-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }

        [FunctionName("AC-priority10")]
        public static async Task Priority10([QueueTrigger("async-caller-priority10-queue", Connection = "AzureWebJobsStorage")] RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            await Distributor.DistributeCall(requestEnvelope, log, context);
        }
    }
}
