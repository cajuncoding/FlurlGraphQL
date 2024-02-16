
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlurlGraphQL.Tests
{
    public abstract class BaseFlurlGraphQLTest
    {
        protected BaseFlurlGraphQLTest()
        {
            ConfigHelpers.InitEnvironmentFromLocalSettingsJson();
            GraphQLApiEndpoint = Environment.GetEnvironmentVariable(nameof(GraphQLApiEndpoint)) ?? throw CreateMissingConfigException(nameof(GraphQLApiEndpoint));
        }

        private Exception CreateMissingConfigException(string configName) => new Exception($"The configuration value for [{configName}] could not be loaded.");

        public string GraphQLApiEndpoint { get; }

        public TestContext TestContext { get; set; } = null!;
    }
}