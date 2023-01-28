using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

#if NETSTANDARD2_1_OR_GREATER

namespace Flurl.Http.GraphQL.Querying
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        /// <summary>
        /// Receives all data paginated as an IEnumerable that you can iterate over each resulting Page.
        /// This uses the formalized Relay spec for Connection pagination: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async IAsyncEnumerable<IGraphQLQueryConnectionResult<TResult>> ReceiveGraphQLQueryConnectionPagesAsyncEnumerable<TResult>(
            this Task<IFlurlResponse> responseTask,
            string queryOperationName = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) where TResult : class
        {
            Task<IFlurlResponse> iterationResponseTask = responseTask;
            //Track our EndCursor to prevent infinite loops due to incorrect query; will be validated.
            string priorEndCursor = null;

            do
            {
                var currentPage = await iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    var originalGraphQLRequest = flurlGraphQLResponse.OriginalGraphQLRequest;

                    var pageResult = responsePayload.LoadTypedResults<TResult>() as IGraphQLQueryConnectionResult<TResult>;

                    //Validate the Page to see if we are able to continue our iteration...
                    var (hasNextPage, endCursor) = AssertCursorPageIsValidForEnumeration(pageResult?.PageInfo, responsePayload, flurlGraphQLResponse, priorEndCursor);

                    //Update our tracking endCursor for validation...
                    priorEndCursor = endCursor;

                    //If there is another page then Update our Variables and request the NEXT Page Asynchronously;
                    //  otherwise set our iteration to null to stop processing!
                    iterationResponseTask = !hasNextPage
                        ? null
                        : originalGraphQLRequest
                            .SetGraphQLVariable(GraphQLArgs.After, endCursor)
                            .PostGraphQLQueryAsync(cancellationToken);

                    //Since this is a Func we must return a value.
                    return pageResult;
                }).ConfigureAwait(false);

                //BBernard
                //Even while we are pre-fetching the next page we can now yield and return the current results to the consumer for processing
                //  creating a pseudo-paginated-stream of async enumerable data with improved performance!
                yield return currentPage;

            } while (iterationResponseTask != null);
        }
    }
}

#endif
