using System.Linq;
using System.Threading.Tasks;
using FlurlGraphQL.Querying.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingPersistedQueryTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public async Task TestPersistedQuerySingleQueryDirectResultsAsync()
        {
            var results = await GraphQLApiEndpoint
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
        public async Task TestPersistedQueryReceiveAllPagesAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1")
                .SetGraphQLVariables(new { first = 2, friendsCount = 1 })
                .PostGraphQLQueryAsync()
                .ReceiveAllGraphQLQueryConnectionPages<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 1);
            var totalCount = results.FirstOrDefault().TotalCount;
            Assert.AreEqual(8, totalCount);

            var allResults = results.SelectMany(p => p).ToList();
            Assert.AreEqual(totalCount, allResults.Count);

            foreach (var character in allResults)
            {
                TestContext.WriteLine($"Received Character: {character.Name}");
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}