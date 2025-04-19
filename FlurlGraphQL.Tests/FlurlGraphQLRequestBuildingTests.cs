using System;
using System.Threading.Tasks;
using Flurl.Http;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using FlurlGraphQL.JsonProcessing;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLRequestBuildingTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public void TestCloneFlurlGraphQLRequest(IFlurlRequest graphqlApiRequest)
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

            var originalRequest = graphqlApiRequest
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
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestExecuteRequestWithCustomNewtonsoftJsonSerializerSettings(IFlurlRequest graphqlApiRequest)
        {
            var response = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($first:Int) {
                      characters (first:$first) {
                        nodes {
                          PERSONALIDENTIFIER: personalIdentifier
                          name
                        }
                      }
                    }
                ")
                .BeforeCall(call =>
                {
                    var serializer = call.Request.Settings.JsonSerializer;
                    Assert.IsTrue(serializer is IFlurlGraphQLNewtonsoftJsonSerializer);
                })
                .UseGraphQLNewtonsoftJson(s =>
                {
                    s.NullValueHandling = NullValueHandling.Ignore;
                    s.Formatting = Formatting.Indented;
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
            foreach (var result in results)
            {
                //Name should be Populated ✅
                Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Name));
                Assert.AreNotEqual(0, result.PersonalIdentifier);
                //Height should be 0 due to not being populated ❎
                Assert.AreEqual(0, result.Height);
            }
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestExecuteRequestWithCustomSystemTextJsonSerializerSettings(IFlurlRequest graphqlApiRequest)
        {
            var response = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($first:Int) {
                      characters (first:$first) {
                        nodes {
                          PERSONALIDENTIFIER: personalIdentifier
                          name
                        }
                      }
                    }
                ")
                .BeforeCall(call =>
                {
                    var serializer = call.Request.Settings.JsonSerializer;
                    Assert.IsTrue(serializer is IFlurlGraphQLSystemTextJsonSerializer);
                })
                .UseGraphQLSystemTextJson(o =>
                {
                    o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    o.WriteIndented = true;
                    //NOTE: THIS is switched to true by the framework, but we are testing that we can NOT explicitly OVERRIDE it HERE!
                    o.PropertyNameCaseInsensitive = false;
                })
                //.SetGraphQLVariable("first", 2)
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ConfigureAwait(false);

            Assert.IsTrue(response.GraphQLRequest is FlurlGraphQLRequest);
            var request = response.GraphQLRequest as FlurlGraphQLRequest;
            Assert.IsTrue(request.GraphQLJsonSerializer is FlurlGraphQLSystemTextJsonSerializer);

            var jsonSerializerOptions = ((FlurlGraphQLSystemTextJsonSerializer)request.GraphQLJsonSerializer).JsonSerializerOptions;
            Assert.IsFalse(jsonSerializerOptions.PropertyNameCaseInsensitive);
            Assert.IsTrue(jsonSerializerOptions.WriteIndented);

            var baseFlurlSerializer = ((IFlurlRequest)request).Settings.JsonSerializer;
            Assert.AreEqual(request.GraphQLJsonSerializer, baseFlurlSerializer);

            var results = await response.ReceiveGraphQLQueryResults<StarWarsCharacter>().ConfigureAwait(false);
            Assert.IsNotNull(results);
            //We still get 2 results but some values are Null due to Case Insensitivity being Disabled!
            Assert.AreEqual(2, results.Count);
            foreach (var result in results)
            {
                //Name should be Populated ✅
                Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Name));
                //PersonalIdentifier & Height should be 0 due to not being populated ❎
                Assert.AreEqual(0, result.PersonalIdentifier);
                Assert.AreEqual(0, result.Height);
            }
        }
    }
}