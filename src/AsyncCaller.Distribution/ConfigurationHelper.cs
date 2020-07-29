using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Nexus.Link.Libraries.Core.Error.Logic;

namespace AsyncCaller.Distribution
{
    public static class ConfigurationHelper
    {
        private static IConfiguration _config;

        public static string GetSetting(string key, ExecutionContext context, bool isMandatory)
        {
            if (_config == null)
            {
                _config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }

            var value = _config[key];
            if (isMandatory)
            {
                // We must not have InternalContract and stuff here, since we may not have set up logging, etc.
                throw new FulcrumContractException($"App setting '{key}' is mandatory, but is missing.");
            }
            return value;
        }

        public static T GetEnum<T>(string key, ExecutionContext context, T defaultIfMissing) where T : struct
        {
            // We must not have InternalContract and stuff here, since we may not have set up logging, etc.
            if (string.IsNullOrWhiteSpace(key)) throw new FulcrumContractException($"Parameter {nameof(key)} was empty.");
            var valueAsString = GetSetting(key, context, false);
            if (string.IsNullOrWhiteSpace(valueAsString)) return defaultIfMissing;
            if (!Enum.TryParse(valueAsString, out T value)) throw new FulcrumContractException($"App setting {key} ({valueAsString}) must have one of the values for {typeof(T).FullName}.");
            return value;
        }
    }
}