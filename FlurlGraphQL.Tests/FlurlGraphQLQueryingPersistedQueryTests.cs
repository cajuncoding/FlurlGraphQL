using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [Ignore] //<== Persisted Queries are not Supported by our Azure Function Integration Test Server; and can only be validated running Locally!
    [TestClass]
    public class FlurlGraphQLQueryingPersistedQueryTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestPersistedPostQuerySingleQueryDirectResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                .SetGraphQLVariables(new { first = 2, friendsCount = 1})
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

			Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            var char1 = results[0];
            Assert.IsNotNull(char1);
            Assert.AreEqual(1000, char1.PersonalIdentifier);
            Assert.AreEqual("Luke Skywalker", char1.Name);
            var friendOfChar1 = char1.Friends.FirstOrDefault();
            Assert.IsNotNull(friendOfChar1);
            Assert.AreEqual("C-3PO", friendOfChar1.Name);

            var char2 = results[1];
            Assert.IsNotNull(char2);
            Assert.AreEqual(1001, char2.PersonalIdentifier);
            Assert.AreEqual("Darth Vader", char2.Name);
            var friendOfChar2 = char2.Friends.FirstOrDefault();
            Assert.IsNotNull(friendOfChar2);
            Assert.AreEqual("Wilhuff Tarkin", friendOfChar2.Name);

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestPersistedPostFailsForInvalidOverrideFieldNameAsync(IFlurlRequest graphqlApiRequest)
        {
            FlurlGraphQLException graphqlException = null;
            //Use Relay Spec default field name (invalid for HotChocolate...
            //More info see: https://chillicream.com/docs/hotchocolate/v13/performance/persisted-queries#client-expectations
            const string RELAY_PERSISTED_QUERY_KEY = "doc_id";
            try
            {
                var request = graphqlApiRequest
                    .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                    .SetPersistedQueryPayloadFieldName(RELAY_PERSISTED_QUERY_KEY)
                    .SetGraphQLVariables(new { first = 2, friendsCount = 1 });

                Assert.AreEqual(RELAY_PERSISTED_QUERY_KEY, request.GraphQLConfig.PersistedQueryPayloadFieldName);

                var results = await request.PostGraphQLQueryAsync()
                    .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                    .ConfigureAwait(false);
            }
            catch (FlurlGraphQLException gqlExc)
            {
                graphqlException = gqlExc;
            }

            Assert.IsNotNull(graphqlException);
            Assert.AreEqual(HttpStatusCode.BadRequest, graphqlException.HttpStatusCode);
            TestContext.WriteLine(graphqlException.Message);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestPersistedPostFailsForInvalidGloballyConfiguredFieldNameAsync(IFlurlRequest graphqlApiRequest)
        {
            FlurlGraphQLException graphqlException = null;
            //Use Relay Spec default field name (invalid for HotChocolate)...
            //More info see: https://chillicream.com/docs/hotchocolate/v13/performance/persisted-queries#client-expectations
            const string RELAY_PERSISTED_QUERY_KEY = "doc_id";

            FlurlGraphQLConfig.ConfigureDefaults(c =>
            {
                c.PersistedQueryPayloadFieldName = RELAY_PERSISTED_QUERY_KEY;
            });

            try
            {
                var request = graphqlApiRequest
                    .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                    .SetGraphQLVariables(new { first = 2, friendsCount = 1 });

                Assert.AreEqual(RELAY_PERSISTED_QUERY_KEY, request.GraphQLConfig.PersistedQueryPayloadFieldName);

                var results = await request.PostGraphQLQueryAsync()
                    .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                    .ConfigureAwait(false);
            }
            catch (FlurlGraphQLException gqlExc)
            {
                graphqlException = gqlExc;
            }

            //We need to RESET our Defaults so we don't affect other Unit Tests...
            FlurlGraphQLConfig.ResetDefaults();

            Assert.IsNotNull(graphqlException);
            Assert.AreEqual(HttpStatusCode.BadRequest, graphqlException.HttpStatusCode);
            TestContext.WriteLine(graphqlException.Message);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestPersistedPostQueryReceiveAllPagesAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                .SetGraphQLVariables(new { first = 2, friendsCount = 1 })
                .PostGraphQLQueryAsync()
                .ReceiveAllGraphQLQueryConnectionPages<StarWarsCharacter>()
                .ConfigureAwait(false);

            AssertPersistedQueryAllPageResultsAreValid(results);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestPersistedGetQueryReceiveAllPagesAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                .SetGraphQLVariables(new { first = 2, friendsCount = 1 })
                .GetGraphQLQueryAsync()
                .ReceiveAllGraphQLQueryConnectionPages<StarWarsCharacter>()
                .ConfigureAwait(false);

            AssertPersistedQueryAllPageResultsAreValid(results);
        }

        private void AssertPersistedQueryAllPageResultsAreValid(IList<IGraphQLConnectionResults<StarWarsCharacter>> results)
        {
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 1);
            var totalCount = results.FirstOrDefault().TotalCount;
            Assert.AreEqual(8, totalCount);

            var allResults = results.SelectMany(p => p).ToList();
            Assert.AreEqual(totalCount, allResults.Count);

            foreach (var character in allResults)
            {
                Assert.IsNotNull(character);
                Assert.IsFalse(string.IsNullOrWhiteSpace(character.Name));
                
                var friendsComment = character.Friends.Any() 
                    ? $"who is friends with [{string.Join(",", character.Friends.Select(f => f.Name))}]"
                    : string.Empty;

                TestContext.WriteLine($"Received Character [{character.Name}] {friendsComment}");
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}