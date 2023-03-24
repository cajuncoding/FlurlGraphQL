using System.Threading.Tasks;
using FlurlGraphQL.Querying.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingPersistedQueryTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public async Task TestSimpleSingleQueryDirectResultsAsync()
        {
            var results = await GraphQLApiEndpoint
                .WithGraphQLPersistedQuery("")
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
    }
}