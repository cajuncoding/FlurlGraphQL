using System;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using FlurlGraphQL.Querying;
using FlurlGraphQL.Querying.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLRequestBuildingTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public void TestCloneFlurlGraphQLRequest()
        {
            var query = @"
                query($first:Int, $after:String) {
                  characters (first:$first, after:$after) {
                    nodes {
                      personalIdentifier
                      name
			          height
                    }
                  }
                }
            ";

            var guidCursor = Guid.NewGuid();

            var originalRequest = GraphQLApiEndpoint
                .WithGraphQLQuery(query)
                //.SetGraphQLVariable("first", 2)
                .SetGraphQLVariables(new { first = 2 })
                .SetGraphQLVariable(GraphQLConnectionArgs.After, guidCursor);

            var clonedRequest = originalRequest.Clone();

            Assert.IsNotNull(originalRequest);
            Assert.IsNotNull(clonedRequest);
            Assert.AreEqual(query, originalRequest.GraphQLQuery);
            Assert.AreEqual(query, clonedRequest.GraphQLQuery);
            Assert.AreEqual(originalRequest.Url, clonedRequest.Url);
            foreach (var kv in originalRequest.GraphQLVariables)
                Assert.AreEqual(kv.Value, clonedRequest.GraphQLVariables[kv.Key]);

            var newVariableName = "newVariable";
            clonedRequest.SetGraphQLVariable(newVariableName, Guid.NewGuid());
            Assert.IsTrue(clonedRequest.GraphQLVariables.ContainsKey(newVariableName));
            Assert.IsFalse(originalRequest.GraphQLVariables.ContainsKey(newVariableName));

            clonedRequest.WithGraphQLQuery("INVALID QUERY TEXT");
            Assert.AreEqual(query, originalRequest.GraphQLQuery);
            Assert.AreNotEqual(query, clonedRequest.GraphQLQuery);
        }

        [TestMethod]
        public async Task TestExecuteRequestWithCustomJsonSerializerSettings()
        {
            var response = await GraphQLApiEndpoint
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
                .SetGraphQLVariables(new { first = 2 })
                .SetGraphQLNewtonsoftJsonSerializerSettings(new JsonSerializerSettings()
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                })
                .PostGraphQLQueryAsync()
                .ConfigureAwait(false);

            Assert.IsTrue(response.GraphQLRequest is FlurlGraphQLRequest);
            var request = response.GraphQLRequest as FlurlGraphQLRequest;
            Assert.IsTrue(request.ContextBag.ContainsKey(nameof(JsonSerializerSettings)));
            var jsonSerializerSettings = (JsonSerializerSettings)request.ContextBag[nameof(JsonSerializerSettings)];
            Assert.AreEqual(Newtonsoft.Json.NullValueHandling.Ignore, jsonSerializerSettings.NullValueHandling);
            Assert.AreEqual(Formatting.Indented, jsonSerializerSettings.Formatting);

            var results = await response.ReceiveGraphQLQueryResults<StarWarsCharacter>().ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
        }
    }
}