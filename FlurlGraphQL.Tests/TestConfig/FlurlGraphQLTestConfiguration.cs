using System;

namespace FlurlGraphQL.Tests.TestConfig
{
    internal class FlurlGraphQLTestConfiguration
    {
        public static bool IsInitialized { get; private set; } = false;

        public static string GraphQLApiEndpoint => GetConfigValue(nameof(GraphQLApiEndpoint));

        private static Exception CreateMissingConfigException(string configName) => new Exception($"The configuration value for [{configName}] could not be loaded.");

        public static string GetConfigValue(string configName)
        {
            InitializeConfig();
            return Environment.GetEnvironmentVariable(configName) ?? throw CreateMissingConfigException(configName);
        }

        public static bool InitializeConfig()
        {
            if(!IsInitialized)
                ConfigHelpers.InitEnvironmentFromLocalSettingsJson();

            return IsInitialized = true;
        }
    }
}
