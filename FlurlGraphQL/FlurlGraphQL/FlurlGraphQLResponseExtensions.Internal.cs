using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FlurlGraphQL
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        //NOTE: THIS Extension is Cloned/Duplicated between teh base FlurlGraphQL and FlurlGraphQL.Newtonsoft libraries as a minimal footprint 
        //      so that the internal extension method is not visible publicly... keeping this as Internal as Possible...
        internal static async Task<TGraphQLResult> ProcessResponsePayloadInternalAsync<TGraphQLResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            Func<IFlurlGraphQLResponseProcessor, FlurlGraphQLResponse, TGraphQLResult> payloadHandlerFunc
        )
        {
            var graphqlResponse = (FlurlGraphQLResponse)await responseTask.ConfigureAwait(false);
            var results = await graphqlResponse.ProcessResponsePayloadInternalAsync(payloadHandlerFunc).ConfigureAwait(false);
            return results;
        }

        internal static async Task<TGraphQLResult> ProcessResponsePayloadInternalAsync<TGraphQLResult>(
            this IFlurlGraphQLResponse response,
            Func<IFlurlGraphQLResponseProcessor, FlurlGraphQLResponse, TGraphQLResult> payloadHandlerFunc
        )
        {
            using (var graphqlResponse = response as FlurlGraphQLResponse)
            {
                if (graphqlResponse == null) return default;

                var responseProcessor = FlurlGraphQLJsonResponseProcessorFactory.FromGraphQLFlurlResponse(graphqlResponse);

                if (responseProcessor.Errors?.Any() ?? false)
                {
                    var responseContent = await graphqlResponse.GetStringAsync().ConfigureAwait(false);
                    throw new FlurlGraphQLException(responseProcessor.Errors, graphqlResponse.GraphQLQuery, responseContent, (HttpStatusCode)graphqlResponse.StatusCode);
                }

                return payloadHandlerFunc.Invoke(responseProcessor, graphqlResponse);
            }
        }

        internal static FlurlGraphQLException NewGraphQLException(IFlurlGraphQLResponseProcessor graphqlResponseProcessor, FlurlGraphQLResponse flurlGraphQLResponse, string message)
            => new FlurlGraphQLException(message, flurlGraphQLResponse.GraphQLQuery, graphqlResponseProcessor, (HttpStatusCode)flurlGraphQLResponse.StatusCode);

        /// <summary>
        /// Internal handler to process the payload in Cursor Pagination Async Enumeration methods... this method supports both:
        ///     - netstandard2.0 IEnumerable&lt;Task&lt;?&gt;&gt; for async enumeration of pages one-by-one (legacy)
        ///     - netstandard2.1+ AsyncEnumerable&lt;?&gt; for true async streaming of pages where the next Page is pre-fetched while yielding the current page.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryOperationName"></param>
        /// <param name="priorEndCursor"></param>
        /// <param name="graphqlResponseProcessor"></param>
        /// <param name="flurlGraphQLResponse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static (
            IGraphQLConnectionResults<TResult> PageResult,
            string UpdatedPriorEndCursor,
            Task<IFlurlGraphQLResponse> NextIterationResponseTask
            ) ProcessPayloadIterationForCursorPaginationAsyncEnumeration<TResult>(
                string queryOperationName,
                string priorEndCursor,
                IFlurlGraphQLResponseProcessor graphqlResponseProcessor,
                FlurlGraphQLResponse flurlGraphQLResponse,
                CancellationToken cancellationToken = default
            ) where TResult : class
        {
            var pageResult = graphqlResponseProcessor.LoadTypedResults<TResult>(queryOperationName) as IGraphQLConnectionResults<TResult>;

            //Validate the Page to see if we are able to continue our iteration...
            var (hasNextPage, endCursor) = AssertCursorPageIsValidForEnumeration(pageResult?.PageInfo, graphqlResponseProcessor, flurlGraphQLResponse, priorEndCursor);

            var originalGraphQLRequest = flurlGraphQLResponse.GraphQLRequest;

            //If there is another page then Update our Variables and request the NEXT Page Asynchronously;
            //  otherwise set our iteration to null to stop processing!
            var iterationResponseTask = !hasNextPage
                ? null
                : originalGraphQLRequest
                    .SetGraphQLVariable(GraphQLConnectionArgs.After, endCursor)
                    .PostGraphQLQueryAsync(cancellationToken);

            //Update our tracking endCursor to the new one we recieved for the next iteration...
            return (pageResult, endCursor, iterationResponseTask);
        }

        /// <summary>
        /// Internal handler to process the payload in Offset Paging Async Enumeration methods... this method supports both:
        ///     - netstandard2.0 IEnumerable&lt;Task&lt;?&gt;&gt; for async enumeration of pages one-by-one (legacy)
        ///     - netstandard2.1+ AsyncEnumerable&lt;?&gt; for true async streaming of pages where the next Page is pre-fetched while yielding the current page.
        /// </summary>
        internal static (
            IGraphQLCollectionSegmentResults<TResult> PageResult,
            Task<IFlurlGraphQLResponse> NextIterationResponseTask
            ) ProcessPayloadIterationForOffsetPaginationAsyncEnumeration<TResult>(
                string queryOperationName,
                IFlurlGraphQLResponseProcessor graphqlResponseProcessor,
                FlurlGraphQLResponse flurlGraphQLResponse,
                CancellationToken cancellationToken = default
            ) where TResult : class
        {
            var originalGraphQLRequest = flurlGraphQLResponse.GraphQLRequest;
            var pageResult = graphqlResponseProcessor.LoadTypedResults<TResult>(queryOperationName) as IGraphQLCollectionSegmentResults<TResult>;

            //Get the current Skip Variable so that we can dynamically increment it to continue the pagination!
            var currentSkipVariable = originalGraphQLRequest.GetGraphQLVariable(GraphQLCollectionSegmentArgs.Skip) as int? ?? 0;

            //Detect if we are safely enumerating and encountered the end of the results
            //NOTE: We must check this before our validation to prevent exceptions for otherwise valid end of results;
            //      in which case we stop the iteration by returning null NextIterationResponseTask along with the null results.
            if (currentSkipVariable > 0 && pageResult != null && !pageResult.HasAnyResults())
                return (null, null);

            var nextSkipVariable = (currentSkipVariable + pageResult?.Count ?? 0);

            //Validate the Page to see if we are able to continue our iteration...
            var hasNextPage = AssertOffsetPageIsValidForEnumeration(pageResult?.PageInfo, graphqlResponseProcessor, flurlGraphQLResponse, nextSkipVariable);

            //If there is another page & this page has results (to be skipped) then Update our Variables and request the NEXT Page;
            //  otherwise set our iteration to null to stop processing!
            var iterationResponseTask = !hasNextPage || !pageResult.HasAnyResults()
                ? null
                : originalGraphQLRequest
                    .SetGraphQLVariable(GraphQLCollectionSegmentArgs.Skip, nextSkipVariable)
                    .PostGraphQLQueryAsync(cancellationToken);

            //Update our tracking endCursor to the new one we received for the next iteration...
            return (pageResult, iterationResponseTask);
        }

        internal static (bool HasNextPage, string EndCursor) AssertCursorPageIsValidForEnumeration(
            IGraphQLCursorPageInfo pageInfo, 
            IFlurlGraphQLResponseProcessor graphqlResponseProcessor, 
            FlurlGraphQLResponse flurlGraphQLResponse, 
            string priorEndCursor
        ) {
            if (pageInfo == null)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo node is missing. Check that the query is correct and that it correctly returns pageInfo.hasNextPage & pageInfo.endCursor values for Cursor based paging.");

            bool? hasNextPageFlag = pageInfo.HasNextPage;
            string endCursor = pageInfo.EndCursor;

            if (hasNextPageFlag == null || endCursor == null)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.hasNextPage and/or the pageInfo.endCursor values are not available in the GraphQL query response.");
            else if (endCursor == priorEndCursor)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.endCursor is returning the same value. Check that the query is correct and that it correctly implements the (after:$after) variable.");

            return (hasNextPageFlag.Value, endCursor);
        }

        internal static bool AssertOffsetPageIsValidForEnumeration(
            IGraphQLOffsetPageInfo pageInfo, 
            IFlurlGraphQLResponseProcessor graphqlResponseProcessor, 
            FlurlGraphQLResponse flurlGraphQLResponse, 
            int skipVariable
        ) {
            if (skipVariable <= 0)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the skip variable is missing. Check that the query is correct and that it correctly implements the (skip: $skip) variable for Offset based paging.");

            if (pageInfo == null)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo node is missing. Check that the query is correct and that it correctly returns pageInfo.hasNextPage value for Offset based paging.");

            bool? hasNextPageFlag = pageInfo?.HasNextPage;
            if (hasNextPageFlag == null)
                throw NewGraphQLException(graphqlResponseProcessor, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.hasNextPage value is not available in the GraphQL query response.");

            return hasNextPageFlag.Value;
        }
    }
}
