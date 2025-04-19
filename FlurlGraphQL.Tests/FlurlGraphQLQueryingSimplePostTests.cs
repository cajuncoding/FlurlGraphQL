using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Flurl.Http;
using FlurlGraphQL.JsonProcessing;
using FlurlGraphQL.SystemTextJsonExtensions;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingSimplePostTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSimplePostSingleQueryDirectResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
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
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSimplePostSingleQueryDirectResultsUsingFragmentsAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query ($ids: [Int!], $friendsCount: Int!) {
	                    charactersById(ids: $ids) {
		                    ...commonFields		
		                    appearsIn
		                    height
		                    friends(first: $friendsCount) {
			                    nodes {
				                    ...commonFields
			                    }
		                    }
	                    }
                    }

                    fragment commonFields on Character {
	                    personalIdentifier
	                    name
                    }
                ")
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
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
            Assert.AreEqual(2, char1.Friends.Count);
            char1.Friends.ForEach(f =>
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(f.Name));
                Assert.IsTrue(f.PersonalIdentifier > 0);
            });


            var char2 = results[1];
            Assert.IsNotNull(char2);
            Assert.AreEqual(2001, char2.PersonalIdentifier);
            Assert.AreEqual("R2-D2", char2.Name);
            Assert.IsTrue(char2.Height > (decimal)1.5);
            Assert.AreEqual(2, char2.Friends.Count);
            char2.Friends.ForEach(f =>
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(f.Name));
                Assert.IsTrue(f.PersonalIdentifier > 0);
            });

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSinglePostQueryRawJsonResponseSystemTextJsonAsync(IFlurlRequest graphqlApiRequest)
        {
            //INTENTIONALLY Place the Nested Paginated selection as LAST item to validate functionality!
            var json = await graphqlApiRequest
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
                //SHOULD also be Default Behavior!
                .UseGraphQLSystemTextJson()
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLRawSystemTextJsonResponse()
                .ConfigureAwait(false);

            Assert.IsNotNull(json);
            Assert.IsTrue(json is JsonObject);
            Assert.AreEqual(2, json["charactersById"]!.AsArray().Count);

            TestContext.WriteLine(json.ToJsonStringIndented());
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSinglePostQueryRawJsonResponseNewtonsoftJsonAsync(IFlurlRequest graphqlApiRequest)
        {
            //INTENTIONALLY Place the Nested Paginated selection as LAST item to validate functionality!
            var json = await graphqlApiRequest
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
                .UseGraphQLNewtonsoftJson()
                .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLRawNewtonsoftJsonResponse()
                .ConfigureAwait(false);

            Assert.IsNotNull(json);
            Assert.IsTrue(json is JToken);
            Assert.AreEqual(2, (json["charactersById"] as JArray)?.Count);

            TestContext.WriteLine(json.ToString(Formatting.Indented));
        }


        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSinglePostQueryWithOnlyNestedPaginatedResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var idArrayParam = new[] { 1000, 2001 };
            var friendCountParam = 1;

            //INTENTIONALLY Place the Nested Paginated selection as LAST item to validate functionality!
            var results = await graphqlApiRequest
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
                .SetGraphQLVariables(new { ids = idArrayParam, friendsCount = friendCountParam })
                .PostGraphQLQueryAsync()
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

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSinglePostQueryWithDoubleNestedPagingResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var idArrayParam = new[] { 1000, 2001 };

            //INTENTIONALLY Place the Nested Paginated selection as FIRST item to validate functionality!
            var results = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query ($ids: [Int!], $firstLevelFriendsCount: Int!, $secondLevelFriendsCount: Int!) {
	                    charactersById(ids: $ids) {
		                    friends(first: $firstLevelFriendsCount) {
			                    nodes {
				                    personalIdentifier
				                    name
				                    friends(first: $secondLevelFriendsCount) {
					                    nodes {
						                    name
						                    personalIdentifier
					                    }
				                    }
			                    }
		                    }
		                    personalIdentifier
		                    name
	                    }
                    }
                ")
                .SetGraphQLVariables(new { 
                    ids = idArrayParam, 
                    firstLevelFriendsCount = 2, 
                    secondLevelFriendsCount = 1
                })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLQueryResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            var index = 0;
            foreach (var result in results)
            {
                Assert.AreEqual(idArrayParam[index], result.PersonalIdentifier);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Name));
                Assert.AreEqual(2, result.Friends.Count);
                foreach (var friend in result.Friends)
                {
                    Assert.AreEqual(1, friend.Friends.Count);
                    Assert.IsTrue(friend.PersonalIdentifier >= 1000);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(result.Name));

                    foreach (var secondLevelFriend in friend.Friends)
                    {
                        Assert.IsTrue(friend.PersonalIdentifier >= 1000);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Name));
                    }
                }
                index++;
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }
}