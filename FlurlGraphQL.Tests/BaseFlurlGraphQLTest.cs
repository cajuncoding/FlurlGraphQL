
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlurlGraphQL.Tests
{
    public abstract class BaseFlurlGraphQLTest
    {
        protected BaseFlurlGraphQLTest()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
            NestedPaginatedStarWarsJsonText = LoadTestData("NestedPaginated.StarWarsData.json");

            ConfigHelpers.InitEnvironmentFromLocalSettingsJson();
            GraphQLApiEndpoint = Environment.GetEnvironmentVariable(nameof(GraphQLApiEndpoint)) ?? throw CreateMissingConfigException(nameof(GraphQLApiEndpoint));
        }

        private Exception CreateMissingConfigException(string configName) => new Exception($"The configuration value for [{configName}] could not be loaded.");

        public string CurrentDirectory { get; }

        public string NestedPaginatedStarWarsJsonText { get; }

        public string GraphQLApiEndpoint { get; }

        public TestContext TestContext { get; set; } = null!;

        public string LoadTestData(string fileName)
        {
            var filePath = Path.Combine(CurrentDirectory, @$"TestData\{fileName}");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The Test Data file [{fileName}] could not be found at: [{filePath}].", fileName);
            return File.ReadAllText(Path.Combine(CurrentDirectory, filePath));
        }
    }
}