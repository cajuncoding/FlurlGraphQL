
using System;
using System.Collections.Generic;
using System.IO;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlurlGraphQL.Tests.TestConfig;

namespace FlurlGraphQL.Tests
{
    public abstract class BaseFlurlGraphQLTest
    {
        protected BaseFlurlGraphQLTest()
        {
            FlurlGraphQLTestConfiguration.InitializeConfig();

            CurrentDirectory = Directory.GetCurrentDirectory();
            NestedPaginatedStarWarsJsonText = LoadTestData("NestedPaginated.StarWarsData.json");
            NestedPaginatedBooksAndAuthorsJsonText = LoadTestData("BooksAndAuthorsCursorPaginatedSmallDataSet.json");
        }

        public string CurrentDirectory { get; }

        public string NestedPaginatedStarWarsJsonText { get; }

        public string NestedPaginatedBooksAndAuthorsJsonText { get; }

        public string GraphQLApiEndpoint => FlurlGraphQLTestConfiguration.GraphQLApiEndpoint;

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