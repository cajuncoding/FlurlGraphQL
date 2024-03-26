using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingPaginatedTests : BaseFlurlGraphQLTest
    {
        [DataTestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryCursorPagingNodeResultsAsync(IFlurlRequest graphQLApiRequest)
        {
            var results = await graphQLApiRequest
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
                .ReceiveGraphQLConnectionResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results is IGraphQLConnectionResults<StarWarsCharacter>);
            Assert.AreEqual(2, results.Count);

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
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryCursorPagingEdgeResultsAndNestedEdgeResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query ($first: Int, $after: String) {
	                    characters(first: $first, after: $after) {
		                    totalCount
		                    pageInfo {
			                    hasNextPage
			                    hasPreviousPage
			                    startCursor
			                    endCursor
		                    }
		                    edges {
			                    cursor
			                    node {
				                    personalIdentifier
				                    name
				                    height
				                    friends(first: $first)
				                    {
					                    edges {
						                    cursor
					                        node
						                    {
							                    personalIdentifier
							                    name
							                    height
						                    }
					                    }
				                    }
			                    }
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { first = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLConnectionResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results is IGraphQLConnectionResults<StarWarsCharacter>);
            Assert.AreEqual(2, results.Count);

            Assert.IsNotNull(results.TotalCount);
            Assert.IsTrue(results.TotalCount > results.Count);
            Assert.IsNotNull(results.PageInfo);
            Assert.IsTrue(results.PageInfo.HasNextPage);
            Assert.IsFalse(results.PageInfo.HasPreviousPage);
            Assert.IsFalse(string.IsNullOrWhiteSpace(results.PageInfo.StartCursor));
            Assert.IsFalse(string.IsNullOrWhiteSpace(results.PageInfo.EndCursor));

            foreach (var result in results)
            {
                Assert.IsNotNull(result);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Cursor));
                foreach (var friend in result.Friends)
                {
                    Assert.IsNotNull(friend);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.Cursor));
                }
            }

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryOffsetPagingResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var results = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($skip:Int, $take:Int) {
                      charactersWithOffsetPaging(skip: $skip, take: $take) {
		                totalCount
                        pageInfo {
                          hasNextPage
                          hasPreviousPage
                        }
                        items {
                          personalIdentifier
                          name
			              height
                        }
                      }
                    }
                ")
                .SetGraphQLVariables(new { skip = 0, take = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLCollectionSegmentResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results is IGraphQLCollectionSegmentResults<StarWarsCharacter>);
            Assert.AreEqual(2, results.Count);

            Assert.IsNotNull(results.TotalCount);
            Assert.IsTrue(results.TotalCount > results.Count);
            Assert.IsNotNull(results.PageInfo);
            Assert.IsTrue(results.PageInfo.HasNextPage);
            Assert.IsFalse(results.PageInfo.HasPreviousPage);

            var char1 = results[0];
            Assert.IsNotNull(char1);

            var jsonText = JsonConvert.SerializeObject(results, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestCursorPagingRetrieveAllPagesAsync(IFlurlRequest graphqlApiRequest)
        {
            var allResultPages = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($first:Int, $after:String) {
                      characters (first:$first, after:$after) {
		                pageInfo {
                          hasNextPage
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
                .ReceiveAllGraphQLQueryConnectionPages<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(allResultPages);
            Assert.IsTrue(allResultPages is IList<IGraphQLConnectionResults<StarWarsCharacter>>);
            Assert.IsTrue(allResultPages.Count > 2);

            foreach (var page in allResultPages)
            {
                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.PageInfo.EndCursor));
                Assert.AreEqual(page != allResultPages.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = allResultPages.SelectMany(p => p);
            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestOffsetPagingRetrieveAllPagesAsync(IFlurlRequest graphqlApiRequest)
        {
            var allResultPages = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($skip:Int, $take:Int) {
                      charactersWithOffsetPaging(skip: $skip, take: $take) {
		                pageInfo {
                          hasNextPage
                        }
                        items {
                          personalIdentifier
                          name
			              height
                        }
                      }
                    }
                ")
                .SetGraphQLVariables(new { skip = 0, take = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveAllGraphQLQueryCollectionSegmentPages<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(allResultPages);
            Assert.IsTrue(allResultPages is IList<IGraphQLCollectionSegmentResults<StarWarsCharacter>>);
            Assert.IsTrue(allResultPages.Count > 2);

            foreach (var page in allResultPages)
            {
                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());
                Assert.AreEqual(page != allResultPages.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = allResultPages.SelectMany(p => p);
            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

#if NET6_0

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestCursorPagingRetrievePagesAsAsyncEnumerableStreamAsync(IFlurlRequest graphqlApiRequest)
        {
            var pagesAsyncEnumerable = graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($first:Int, $after:String) {
                      characters (first:$first, after:$after) {
		                pageInfo {
                          hasPreviousPage
                          hasNextPage
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
                .ReceiveGraphQLConnectionPagesAsyncEnumerable<StarWarsCharacter>();

            Assert.IsNotNull(pagesAsyncEnumerable);
            Assert.IsTrue(pagesAsyncEnumerable is IAsyncEnumerable<IGraphQLConnectionResults<StarWarsCharacter>>);

            var pageResultsList = new List<IGraphQLConnectionResults<StarWarsCharacter>>();

            //Streaming our pages...
            await foreach (var page in pagesAsyncEnumerable.ConfigureAwait(false))
            {
                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.PageInfo.EndCursor));

                //Aggregate into a List for additional validation...
                pageResultsList.Add(page);
            }

            //Additional validation now that we have all pages streamed into memory...
            foreach (var page in pageResultsList)
            {
                Assert.AreEqual(page != pageResultsList.First(), page.PageInfo.HasPreviousPage);
                Assert.AreEqual(page != pageResultsList.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = pageResultsList.SelectMany(p => p);
            Assert.IsTrue(allResults.Count() > 2);

            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestOffsetPagingRetrievePagesAsAsyncEnumerableStreamAsync(IFlurlRequest graphqlApiRequest)
        {
            var pagesAsyncEnumerable = graphqlApiRequest
                .WithGraphQLQuery(@"
                    query ($skip: Int, $take: Int) {
	                    charactersWithOffsetPaging(skip: $skip, take: $take) {
		                    pageInfo {
			                    hasNextPage
			                    hasPreviousPage
		                    }
		                    items {
			                    personalIdentifier
			                    name
			                    height
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { take = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLCollectionSegmentPagesAsyncEnumerable<StarWarsCharacter>();

            Assert.IsNotNull(pagesAsyncEnumerable);
            Assert.IsTrue(pagesAsyncEnumerable is IAsyncEnumerable<IGraphQLCollectionSegmentResults<StarWarsCharacter>>);

            var pageResultsList = new List<IGraphQLCollectionSegmentResults<StarWarsCharacter>>();

            //Streaming our pages...
            await foreach (var page in pagesAsyncEnumerable.ConfigureAwait(false))
            {
                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());

                //Aggregate into a List for additional validation...
                pageResultsList.Add(page);
            }

            //Additional validation now that we have all pages streamed into memory...
            foreach (var page in pageResultsList)
            {
                Assert.AreEqual(page != pageResultsList.First(), page.PageInfo.HasPreviousPage);
                Assert.AreEqual(page != pageResultsList.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = pageResultsList.SelectMany(p => p);
            Assert.IsTrue(allResults.Count() > 2);

            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

#endif

#if NET461

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestCursorPagingRetrievePagesAsEnumerableTasksAsync(IFlurlRequest graphqlApiRequest)
        {
            var enumerablePageTasks = graphqlApiRequest
                .WithGraphQLQuery(@"
                    query($first:Int, $after:String) {
                      characters (first:$first, after:$after) {
		                pageInfo {
                          hasNextPage
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
                .ReceiveGraphQLConnectionPagesAsEnumerableTasks<StarWarsCharacter>();

            Assert.IsNotNull(enumerablePageTasks);
            Assert.IsTrue(enumerablePageTasks is IEnumerable<Task<IGraphQLConnectionResults<StarWarsCharacter>>>);

            List<IGraphQLConnectionResults<StarWarsCharacter>> pageResultsList = new List<IGraphQLConnectionResults<StarWarsCharacter>>();

            //Enumerate the async retrieved pages (as Tasks)...
            foreach (var pageTask in enumerablePageTasks)
            {
                var page = await pageTask.ConfigureAwait(false);

                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.PageInfo.EndCursor));

                //Aggregate into a List for additional validation...
                pageResultsList.Add(page);
            }

            //Additional validation now that we have all pages streamed into memory...
            foreach (var page in pageResultsList)
            {
                Assert.AreEqual(page != pageResultsList.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = pageResultsList.SelectMany(p => p);
            Assert.IsTrue(allResults.Count() > 2);

            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestOffsetPagingRetrievePagesAsEnumerableTasksAsync(IFlurlRequest graphqlApiRequest)
        {
            var enumerablePageTasks = graphqlApiRequest
                .WithGraphQLQuery(@"
                    query ($skip: Int, $take: Int) {
	                    charactersWithOffsetPaging(skip: $skip, take: $take) {
		                    pageInfo {
			                    hasNextPage
			                    hasPreviousPage
		                    }
		                    items {
			                    personalIdentifier
			                    name
			                    height
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { take = 2 })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLCollectionSegmentPagesAsEnumerableTasks<StarWarsCharacter>();

            Assert.IsNotNull(enumerablePageTasks);
            Assert.IsTrue(enumerablePageTasks is IEnumerable<Task<IGraphQLCollectionSegmentResults<StarWarsCharacter>>>);

            var pageResultsList = new List<IGraphQLCollectionSegmentResults<StarWarsCharacter>>();

            //Enumerate the async retrieved pages (as Tasks)...
            foreach (var pageTask in enumerablePageTasks)
            {
                var page = await pageTask.ConfigureAwait(false);

                Assert.IsNotNull(page);
                Assert.IsTrue(page.HasAnyResults());
                Assert.IsFalse(page.HasTotalCount());

                //Aggregate into a List for additional validation...
                pageResultsList.Add(page);
            }

            //Additional validation now that we have all pages streamed into memory...
            foreach (var page in pageResultsList)
            {
                Assert.AreEqual(page != pageResultsList.First(), page.PageInfo.HasPreviousPage);
                Assert.AreEqual(page != pageResultsList.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = pageResultsList.SelectMany(p => p);
            Assert.IsTrue(allResults.Count() > 2);

            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

#endif

    }
}