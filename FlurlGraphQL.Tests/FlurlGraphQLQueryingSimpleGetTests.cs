using System.Threading.Tasks;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingSimpleGetTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public async Task TestSimpleGetSingleQueryDirectResultsAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query ($ids: [Int!]) {
	                    charactersById(ids: $ids) {
		                    personalIdentifier
		                    name
		                    height
	                    }
                    }
                ")
                .SetGraphQLVariables(new { ids = new[] {1000, 2001}})
                .GetGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

			Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            var char1 = results[0];
            Assert.IsNotNull(char1);
            Assert.AreEqual(1000, char1.PersonalIdentifier);
            Assert.AreEqual("Luke Skywalker", char1.Name);
            Assert.IsTrue(char1.Height > (decimal)1.5);

            var char2 = results[1];
            Assert.IsNotNull(char2);
            Assert.AreEqual(2001, char2.PersonalIdentifier);
            Assert.AreEqual("R2-D2", char2.Name);
            Assert.IsTrue(char2.Height > (decimal)1.5);

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }


        [TestMethod]
        public async Task TestSingleQueryWithOnlyNestedPaginatedResultsAsync()
        {
            var idArrayParam = new[] { 1000, 2001 };
            var friendCountParam = 1;

            //INTENTIONALLY Place the Nested Paginated selection as LAST item to validate functionality!
            var results = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query ($ids: [Int!], $friendsCount: Int!) {
	                    charactersById(ids: $ids) {
		                    friends(first: $friendsCount) {
			                    nodes {
				                    friends(first: $friendsCount) {
					                    nodes {
						                    name
						                    personalIdentifier
					                    }
				                    }
			                    }
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = friendCountParam })
                .GetGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            foreach (var result in results)
            {
                Assert.AreEqual(friendCountParam, result.Friends.Count);
                foreach (var friend in result.Friends)
                {
                    Assert.AreEqual(friendCountParam, friend.Friends.Count);
                }
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}