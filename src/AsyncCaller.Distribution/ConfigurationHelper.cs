using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace AsyncCaller.Distribution
{
    public static class ConfigurationHelper
    {
        private static IConfigurationRoot _config;

        public static string GetSetting(string key, ExecutionContext context)
        {
            if (_config == null)
            {
                _config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }

            return _config[key];
        }
    }
}