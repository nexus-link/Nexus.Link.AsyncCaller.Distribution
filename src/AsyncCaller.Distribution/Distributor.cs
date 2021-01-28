using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
            httpClient.Timeout = TimeSpan.FromSeconds(120); // Must be longer than AC timeout, which is 100 s
            HttpSender = new HttpClientWrapper(httpClient);
        }

        public static async Task DistributeCall(RawRequestEnvelope requestEnvelope, ILogger log)
        {
            try
            {
                // Setup correlation id
                MaybeSetupCorrelationId(requestEnvelope, log);

                // Log this invocation
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
                var errorMessage = "Failed to distribute request. (Code location 5ADA0B3E-2344-4977-922B-F7BB870EA065)" +
                                   $" | Loaded configurations: {string.Join(", ", Startup.AsyncCallerServiceConfiguration.Keys)}";
                log.LogError(e, errorMessage);
                Log.LogError(errorMessage, e);
                throw;
            }
        }

        private static readonly Regex CorrelationIdRegex = new Regex(@"X-Correlation-ID: ([^\s]+)", RegexOptions.IgnoreCase);

        private static void MaybeSetupCorrelationId(RawRequestEnvelope requestEnvelope, ILogger log)
        {
            if (!string.IsNullOrWhiteSpace(FulcrumApplication.Context.CorrelationId)) return;

            try
            {
                var callOut = Encoding.UTF8.GetString(requestEnvelope.RawRequest.CallOut);
                var correlationIdMatch = CorrelationIdRegex.Match(callOut);
                if (correlationIdMatch.Success)
                {
                    FulcrumApplication.Context.CorrelationId = correlationIdMatch.Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                const string message = "Error when finding correlation id";
                log.LogWarning(message, e);
                Log.LogWarning(message, e);
            }
        }
    }
}