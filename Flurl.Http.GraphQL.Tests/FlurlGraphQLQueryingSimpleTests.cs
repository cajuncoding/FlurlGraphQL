using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http.GraphQL.Querying;
using Flurl.Http.GraphQL.Querying.NewtonsoftJson;
using Flurl.Http.GraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingSimpleTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public async Task TestSimpleSingleQueryDirectResultsAsync()
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
                .PostGraphQLQueryAsync()
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
        public async Task TestSingleQueryRawJsonResponseAsync()
        {
            var json = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query ($ids: [Int!], $friendsCount: Int!) {
	                    charactersById(ids: $ids) {
		                    personalIdentifier
		                    name
		                    friends(first: $friendsCount) {
			                    nodes {
				                    personalIdentifier
				                    name
			                    }
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLRawJsonResponse()
                .ConfigureAwait(false);

            Assert.IsNotNull(json);
            Assert.AreEqual(2, (json["charactersById"] as JArray)?.Count);

            var jsonText = json.ToString(Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        public async Task TestSingleQueryWithDoubleNestedPagingResultsAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query ($ids: [Int!], $friendsCount: Int!) {
	                    charactersById(ids: $ids) {
		                    personalIdentifier
		                    name
		                    friends(first: $friendsCount) {
			                    nodes {
				                    personalIdentifier
				                    name
				                    friends(first: 1) {
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
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            foreach (var result in results)
            {
                Assert.AreEqual(2, result.Friends.Count);
                foreach (var friend in result.Friends)
                {
                    Assert.AreEqual(1, friend.Friends.Count);
                }
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}