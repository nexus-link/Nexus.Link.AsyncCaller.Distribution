using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Nexus.Link.AsyncCaller.Dispatcher.Logic;
using Nexus.Link.Configurations.Sdk;
using Nexus.Link.Libraries.Core.Application;
using Nexus.Link.Libraries.Core.Assert;
using Nexus.Link.Libraries.Core.MultiTenant.Model;
using Nexus.Link.Libraries.Core.Platform.Authentication;
using Nexus.Link.Libraries.Core.Platform.Configurations;
using Nexus.Link.Libraries.Web.Pipe.Outbound;
using Nexus.Link.Libraries.Web.RestClientHelper;
using Nexus.Link.Logger.Sdk;
using Nexus.Link.Logger.Sdk.RestClients;
using Xlent.Lever.AsyncCaller.Data.Models;
using FulcrumApplicationHelper = Nexus.Link.Libraries.Web.AspNet.Application.FulcrumApplicationHelper;

namespace AsyncCaller.Distribution
{
    public static class Distributor
    {
        public static ILeverServiceConfiguration AsyncCallerServiceConfiguration { get; set; }
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

        public static async Task DistributeCall(RequestEnvelope requestEnvelope, ILogger log, ExecutionContext context)
        {
            try
            {
                SetupFulcrumApplication(context);
                InternalContract.RequireNotNull(AsyncCallerServiceConfiguration, nameof(AsyncCallerServiceConfiguration));

                var clientTenant = new Tenant(requestEnvelope.Organization, requestEnvelope.Environment);
                ServiceContract.RequireValidated(clientTenant, nameof(clientTenant));
                FulcrumApplication.Context.ClientTenant = clientTenant;

                var clientConfig = await AsyncCallerServiceConfiguration.GetConfigurationForAsync(clientTenant);
                FulcrumApplication.Context.LeverConfiguration = clientConfig;

                var handler = new RequestHandler(HttpSender, clientTenant, FulcrumApplication.Context.LeverConfiguration, requestEnvelope);
                await handler.ProcessOneRequestAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to distribute request. (Code location 5ADA0B3E-2344-4977-922B-F7BB870EA065)");
                throw;
            }
        }

        private static void SetupFulcrumApplication(ExecutionContext context)
        {
            if (AsyncCallerServiceConfiguration == null)
            {
                var serviceOrganization = ConfigurationHelper.GetSetting("Nexus:Organization", context, true);
                var serviceEnvironment = ConfigurationHelper.GetSetting("Nexus:Environment", context, true);
                var serviceTenant = new Tenant(serviceOrganization, serviceEnvironment);
                ServiceContract.RequireValidated(serviceTenant, nameof(serviceTenant));

                var nexusFundamentalsUrl = ConfigurationHelper.GetSetting("Nexus:FundamentalsUrl", context, true);
                var clientId = ConfigurationHelper.GetSetting("Nexus:Authentication:ClientId", context, true);
                var clientSecret = ConfigurationHelper.GetSetting("Nexus:Authentication:ClientSecret", context, true);
                var nexusServiceCredentials = new AuthenticationCredentials { ClientId = clientId, ClientSecret = clientSecret };

                var runtimeLevel = ConfigurationHelper.GetEnum("Nexus:RunTimeLevel", context, RunTimeLevelEnum.Production);

                FulcrumApplicationHelper.WebBasicSetup($"async-caller-function-app-{serviceTenant.Organization}-{serviceTenant.Environment}", serviceTenant, runtimeLevel);

                var loggingConfiguration = new LeverServiceConfiguration(serviceTenant, "logging", nexusFundamentalsUrl, nexusServiceCredentials, nexusFundamentalsUrl);
                var logClient = new LogClient("http://this.will.be.ignored", new BasicAuthenticationCredentials());
                var logger = new FulcrumLogger(logClient, loggingConfiguration);
                FulcrumApplication.Setup.SynchronousFastLogger = logger;

                AsyncCallerServiceConfiguration = new LeverServiceConfiguration(serviceTenant, "AsyncCaller", nexusFundamentalsUrl, nexusServiceCredentials, nexusFundamentalsUrl);
            }
        }
    }
}