using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http.GraphQL.Querying;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Flurl.Http.GraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingTests : BaseFlurlGraphQLTest
    {
        // ReSharper disable once ClassNeverInstantiated.Local
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
            Assert.IsTrue(results is IGraphQLConnectionResults<StarWarsCharacter>);
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
        public async Task TestCursorPagingRetrieveAllPagesAsync()
        {
            var allResultPages = await GraphQLApiEndpoint
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
            Assert.IsTrue(allResultPages.Count > 0);

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

        #if NET6_0

        [TestMethod]
        public async Task TestCursorPagingRetrievePagesAsAsyncEnumerableStreamAsync()
        {
            var pagesAsyncEnumerable = GraphQLApiEndpoint
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
                Assert.AreEqual(page != pageResultsList.Last(), page.PageInfo.HasNextPage);
            }

            //Flatten the Page Results to a single set of results via Linq
            var allResults = pageResultsList.SelectMany(p => p);
            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        #endif

        [TestMethod]
        public async Task TestSingleQueryOffsetPagingResultsAsync()
        {
            var results = await GraphQLApiEndpoint
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
                .ReceiveGraphQLQueryCollectionSegmentResults<StarWarsCharacter>()
                .ConfigureAwait(false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results is IGraphQLCollectionSegmentResults<StarWarsCharacter>);
            Assert.IsTrue(results.Count > 0);

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
        public async Task TestOffsetPagingRetrieveAllPagesAsync()
        {
            var allResultPages = await GraphQLApiEndpoint
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
            Assert.IsTrue(allResultPages.Count > 0);

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

        #if NET48

        [TestMethod]
        public async Task TestOffsetPagingRetrievePagesAsEnumerableAsync()
        {
            var enumerablePageTasks = GraphQLApiEndpoint
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
            var jsonText = JsonConvert.SerializeObject(allResults, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }

        #endif

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