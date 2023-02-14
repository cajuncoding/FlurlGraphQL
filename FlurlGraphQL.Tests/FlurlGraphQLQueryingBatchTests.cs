using System.Threading.Tasks;
using FlurlGraphQL.Querying.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingBatchTests : BaseFlurlGraphQLTest
    {
 
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