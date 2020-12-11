using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Link.AsyncCaller.Sdk.Data.Models;
using Nexus.Link.AsyncCaller.Sdk.Dispatcher.Logic;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.Assert;
using Nexus.Link.Libraries.Core.Error.Logic;
using Nexus.Link.Libraries.Core.Logging;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Web.Pipe.Outbound;
using Nexus.Link.Libraries.Web.RestClientHelper;

namespace AsyncCaller.Distribution
{
    public static class Distributor
    {
        public static IHttpClient HttpSender { get; set; }

        static Distributor()
        {
            var handlers = new List<DelegatingHandler>
            {
                new LogRequestAndResponse()
            };

            var httpClient = HttpClientFactory.Create(handlers.ToArray());
            HttpSender = new HttpClientWrapper(httpClient);
        }

        public static async Task DistributeCall(RawRequestEnvelope requestEnvelope, ILogger log)
        {
            try
            {
                // Log invocation
                var invocationLogMessage = $"{requestEnvelope}; prio: {requestEnvelope.RawRequest.Priority?.ToString() ?? "standard"}";
                log.LogDebug(invocationLogMessage);
                Log.LogVerbose(invocationLogMessage);

                InternalContract.RequireNotNull(Startup.AsyncCallerServiceConfiguration, nameof(Startup.AsyncCallerServiceConfiguration),
                    $"Missing {nameof(Startup.AsyncCallerServiceConfiguration)}. Please check your Nexus configuration for this function app.");

                // Client tenant is found in the request envelope
                var clientTenant = new Tenant(requestEnvelope.Organization, requestEnvelope.Environment);
                ServiceContract.RequireValidated(clientTenant, nameof(clientTenant));
                FulcrumApplication.Context.ClientTenant = clientTenant;

                // Setup the tenants AC configuration (cache and refresh is handled by LeverServiceConfiguration)
                if (!Startup.AsyncCallerServiceConfiguration.TryGetValue(clientTenant, out var serviceConfiguration))
                {
                    throw new FulcrumUnauthorizedException($"Unable to find credentials for fetching Nexus configuration for tenant {clientTenant}");
                }
                var clientConfig = await serviceConfiguration.GetConfigurationForAsync(clientTenant);
                FulcrumApplication.Context.LeverConfiguration = clientConfig;

                // Distribute the request. RequestHandler will put back on queue if necessary, and also handle callbacks
                var handler = new RequestHandler(HttpSender, clientTenant, FulcrumApplication.Context.LeverConfiguration, requestEnvelope);
                await handler.ProcessOneRequestAsync();
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to distribute request. (Code location 5ADA0B3E-2344-4977-922B-F7BB870EA065)";
                log.LogError(e, errorMessage);
                Log.LogError(errorMessage, e);
                throw;
            }
        }
    }
}