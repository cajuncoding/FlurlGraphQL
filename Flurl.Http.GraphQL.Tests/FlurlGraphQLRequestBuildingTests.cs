using System;
using Flurl.Http.GraphQL.Querying;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Flurl.Http.GraphQL.Tests
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
    }
}