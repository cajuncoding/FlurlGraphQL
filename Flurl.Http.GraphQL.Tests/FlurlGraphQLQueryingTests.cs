using Flurl.Http;
using Flurl.Http.GraphQL.Querying;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingTests : BaseFlurlGraphQLTest
    {
        private class StarWarsCharacter
        {
            public int PersonalIdentifier { get; set; }
            public string Name { get; set; }
            public decimal Height { get; set; }
        }

        [TestMethod]
        public async Task TestSimpleSingleQueryDirectResultsAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query($first:Int) {
                      characters (first:$first) {
                        nodes {
                          personalIdentifier
                          name
			              height
                        }
                      }
                    }
                ")
                //.SetGraphQLVariable("first", 2)
                .SetGraphQLVariables(new { first = 2})
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

			Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);

            var char1 = results[0];
            Assert.IsNotNull(char1);
            Assert.IsTrue(char1.PersonalIdentifier >= 1000);
            Assert.IsNotNull(char1.Name);
            Assert.IsTrue(char1.Height > (decimal)1.5);

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        public async Task TestSingleQueryCursorPagingResultsAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query($first:Int) {
                      characters (first:$first) {
                        totalCount
		                pageInfo {
                          hasNextPage
                          hasPreviousPage
                          startCursor
                          endCursor
                        }
                        nodes {
                          personalIdentifier
                          name
			              height
                        }
                      }
                    }
                ")
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLQueryConnectionResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results is GraphQLQueryConnectionResult<StarWarsCharacter>);
            Assert.IsTrue(results.Count > 0);

            Assert.IsNotNull(results.TotalCount);
            Assert.IsTrue(results.TotalCount > results.Count);
            Assert.IsNotNull(results.PageInfo);
            Assert.IsTrue(results.PageInfo.HasNextPage);
            Assert.IsFalse(results.PageInfo.HasPreviousPage);
            Assert.IsFalse(string.IsNullOrWhiteSpace(results.PageInfo.StartCursor));
            Assert.IsFalse(string.IsNullOrWhiteSpace(results.PageInfo.EndCursor));

            var char1 = results[0];
            Assert.IsNotNull(char1);

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        public async Task TestBatchQueryDirectResultsAsync()
        {
            var batchResults = await GraphQLApiEndpoint
                .WithGraphQLQuery(@"
                    query ($first: Int) {
	                    characters(first: $first) {
		                    nodes {
			                    personalIdentifier
			                    name
			                    height
		                    }
	                    }

	                    charactersCount: characters {
		                    totalCount
	                    }
                    }
                ")
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLBatchQueryResults()
                .ConfigureAwait(false);


            Assert.IsNotNull(batchResults);
            Assert.IsTrue(batchResults.Count > 0);

            var resultByName = batchResults.GetResults<StarWarsCharacter>("characters");
            var resultByIndex = batchResults.GetResults<StarWarsCharacter>(0);
            Assert.AreEqual(resultByName, resultByIndex);

            var char1 = resultByName[0];
            Assert.IsNotNull(char1);
            Assert.IsTrue(char1.PersonalIdentifier >= 1000);
            Assert.IsNotNull(char1.Name);
            Assert.IsTrue(char1.Height > (decimal)1.5);

            var countResult = batchResults.GetConnectionResults<StarWarsCharacter>("charactersCount");
            Assert.IsNotNull(countResult);
            Assert.IsTrue(countResult.TotalCount > resultByName.Count);

            var jsonText = JsonConvert.SerializeObject(batchResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}