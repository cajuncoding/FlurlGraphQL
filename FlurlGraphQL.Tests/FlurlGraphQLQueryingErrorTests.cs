using System;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLQueryingErrorTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryErrorForBadRequestQueryAsync(IFlurlRequest graphqlApiRequest)
        {
            var exc = await ExecuteAndCaptureException(async () =>
            {
                var json = await graphqlApiRequest
                    .WithGraphQLQuery(@"
                        query (BAD_REQUEST) {
	                        MALFORMED QUERY
                        }
                    ")
                    .PostGraphQLQueryAsync()
                    .ReceiveGraphQLRawSystemTextJsonResponse()
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

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryErrorForMalformedQueryAsync(IFlurlRequest graphqlApiRequest)
        {
            var exc = await ExecuteAndCaptureException(async () =>
            {
                var json = await graphqlApiRequest
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
                    .ReceiveGraphQLRawSystemTextJsonResponse()
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

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestSingleQueryErrorForInvalidSelectionAsync(IFlurlRequest graphqlApiRequest)
        {
            var exc = await ExecuteAndCaptureException(async () =>
            {
                var json = await graphqlApiRequest
                    .WithGraphQLQuery(@"
                        query ($ids: [Int!]) {
	                        charactersById(ids: $ids) {
		                        personalIdentifierNOT_VALID
		                        name
	                        }
                        }
                    ")
                    .SetGraphQLVariables(new { ids = new[] { 1000, 2001 } })
                    .PostGraphQLQueryAsync()
                    .ReceiveGraphQLRawSystemTextJsonResponse()
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