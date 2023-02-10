using System;
using System.Threading.Tasks;
using Flurl.Http.GraphQL.Querying;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flurl.Http.GraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingErrorTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public async Task TestSingleQueryErrorForBadRequestQueryAsync()
        {
            var exc = await ExecuteAndCaptureException(async () =>
            {
                var json = await GraphQLApiEndpoint
                    .WithGraphQLQuery(@"
                        query (BAD_REQUEST) {
	                        MALFORMED QUERY
                        }
                    ")
                    .PostGraphQLQueryAsync()
                    .ReceiveGraphQLRawJsonResponse()
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.IsNotNull(exc);
            var graphqlException = exc as FlurlGraphQLException;
            Assert.IsNotNull(graphqlException);
            Assert.IsNotNull(graphqlException.Query);
            Assert.IsNotNull(graphqlException.InnerException);
            Assert.IsNull(graphqlException.ErrorResponseContent);
            Assert.IsNull(graphqlException.GraphQLErrors);

            TestContext.WriteLine(graphqlException.Message);
        }

        [TestMethod]
        public async Task TestSingleQueryErrorForMalformedQueryAsync()
        {
            var exc = await ExecuteAndCaptureException(async () =>
            {
                var json = await GraphQLApiEndpoint
                    .WithGraphQLQuery(@"
                        query ($ids: [Int!], $friendsCount: Int!) {
	                        charactersById(ids: $ids) {
		                        personalIdentifier
		                        name
	                        }
                        }
                    ")
                    .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }})
                    .PostGraphQLQueryAsync()
                    .ReceiveGraphQLRawJsonResponse()
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.IsNotNull(exc);
            var graphqlException = exc as FlurlGraphQLException;
            Assert.IsNotNull(graphqlException);
            Assert.IsNotNull(graphqlException.Query);
            Assert.IsNotNull(graphqlException.ErrorResponseContent);
            Assert.IsNotNull(graphqlException.GraphQLErrors);
            Assert.IsNotNull(graphqlException.InnerException);

            TestContext.WriteLine(graphqlException.Message);
        }

        private async Task<Exception> ExecuteAndCaptureException(Func<Task> exceptionFunc)
        {
            try
            {
                await exceptionFunc?.Invoke();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }
    }
}