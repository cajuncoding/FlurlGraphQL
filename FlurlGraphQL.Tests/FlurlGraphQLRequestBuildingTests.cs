using System;
using System.Threading.Tasks;
using Flurl.Http;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlurlGraphQL.JsonProcessing;

namespace FlurlGraphQL.Tests
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
        public async Task TestExecuteRequestWithCustomNewtonsoftJsonSerializerSettings()
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
                .UseGraphQLNewtonsoftJson(new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                })
                //.SetGraphQLVariable("first", 2)
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ConfigureAwait(false);

            Assert.IsTrue(response.GraphQLRequest is FlurlGraphQLRequest);
            var request = response.GraphQLRequest as FlurlGraphQLRequest;
            Assert.IsTrue(request.GraphQLJsonSerializer is FlurlGraphQLNewtonsoftJsonSerializer);

            var jsonSerializerSettings = ((FlurlGraphQLNewtonsoftJsonSerializer)request.GraphQLJsonSerializer).JsonSerializerSettings;
            Assert.AreEqual(Newtonsoft.Json.NullValueHandling.Ignore, jsonSerializerSettings.NullValueHandling);
            Assert.AreEqual(Formatting.Indented, jsonSerializerSettings.Formatting);

            var baseFlurlSerializer = ((IFlurlRequest)request).Settings.JsonSerializer;
            Assert.AreEqual(request.GraphQLJsonSerializer, baseFlurlSerializer);

            var results = await response.ReceiveGraphQLQueryResults<StarWarsCharacter>().ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.IsNotNull(results[0].Name);
            Assert.IsNotNull(results[0].Name);
        }

        [TestMethod]
        public async Task TestExecuteRequestWithCustomSystemTextJsonSerializerSettings()
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
                .UseGraphQLSystemTextJson(new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                    //NOTE: THIS is switched to true by the framework but we are testing that we can explicitly OVERRIDE it HERE!
                    PropertyNameCaseInsensitive = false
                })
                //.SetGraphQLVariable("first", 2)
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ConfigureAwait(false);

            Assert.IsTrue(response.GraphQLRequest is FlurlGraphQLRequest);
            var request = response.GraphQLRequest as FlurlGraphQLRequest;
            Assert.IsTrue(request.GraphQLJsonSerializer is FlurlGraphQLSystemTextJsonSerializer);

            var jsonSerializerOptions = ((FlurlGraphQLSystemTextJsonSerializer)request.GraphQLJsonSerializer).JsonSerializerOptions;
            Assert.AreEqual(false, jsonSerializerOptions.PropertyNameCaseInsensitive);
            Assert.AreEqual(true, jsonSerializerOptions.WriteIndented);

            var baseFlurlSerializer = ((IFlurlRequest)request).Settings.JsonSerializer;
            Assert.AreEqual(request.GraphQLJsonSerializer, baseFlurlSerializer);

            var results = await response.ReceiveGraphQLQueryResults<StarWarsCharacter>().ConfigureAwait(false);
            Assert.IsNotNull(results);
            //We still get 2 results but the values are Null...
            Assert.AreEqual(2, results.Count);
            Assert.IsNull(results[0].Name);
            Assert.IsNull(results[0].Name);
        }
    }
}