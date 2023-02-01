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
        /// This will automatically enumerate through ALL possible page results, using the GraphQL query, as a Stream via IAsyncEnumerable.
        /// This offers true async streaming in that while you handle one page it's already pre-fetching the next page asynchronously.
        /// This is great for streaming data from GraphQL to another destination (e.g. a Database).
        /// The GraphQL query MUST support the (after: $after) variable, and return pageInfo.hasNextPage & pageInfo.endCursor in the results for Cursor pagination!
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns an IAsyncEnumerable of IGraphQLQueryConnectionResult sets containing the typed results along with paging information returned by the query.</returns>
        public static async IAsyncEnumerable<IGraphQLConnectionResults<TResult>> ReceiveGraphQLConnectionPagesAsyncEnumerable<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) where TResult : class
        {
            Task<IFlurlGraphQLResponse> iterationResponseTask = responseTask;
            //Track our EndCursor to prevent infinite loops due to incorrect query; will be validated.
            string priorEndCursor = null;

            do
            {
                var currentPage = await iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    IGraphQLConnectionResults<TResult> pageResult;
                    (pageResult, priorEndCursor, iterationResponseTask) = ProcessPayloadIterationForCursorPaginationAsyncEnumeration<TResult>(
                        queryOperationName,
                        priorEndCursor,
                        responsePayload,
                        flurlGraphQLResponse,
                        cancellationToken
                    );

                    return pageResult;
                }).ConfigureAwait(false);

                //BBernard
                //Even while we are pre-fetching the next page we can now yield and return the current results to the consumer for processing
                //  creating a true async paginated-stream of async enumerable data with improved performance!
                yield return currentPage;

            } while (iterationResponseTask != null);
        }

        /// <summary>
        /// This will automatically enumerate through ALL possible page results, using the GraphQL query, as a Stream via IAsyncEnumerable.
        /// This offers true async streaming in that while you handle one page it's already pre-fetching the next page asynchronously.
        /// This is great for streaming data from GraphQL to another destination (e.g. a Database).
        /// The GraphQL query MUST support the (skip: $skip) variable, and return pageInfo.hasNextPage in the results for Offset pagination!
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns an IAsyncEnumerable of IGraphQLQueryConnectionResult sets containing the typed results along with paging information returned by the query.</returns>
        public static async IAsyncEnumerable<IGraphQLCollectionSegmentResults<TResult>> ReceiveGraphQLCollectionSegmentPagesAsyncEnumerable<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) where TResult : class
        {
            Task<IFlurlGraphQLResponse> iterationResponseTask = responseTask;

            do
            {
                var currentPage = await iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    IGraphQLCollectionSegmentResults<TResult> pageResult;
                    (pageResult, iterationResponseTask) = ProcessPayloadIterationForOffsetPaginationAsyncEnumeration<TResult>(
                        queryOperationName,
                        responsePayload,
                        flurlGraphQLResponse,
                        cancellationToken
                    );

                    return pageResult;
                }).ConfigureAwait(false);

                //BBernard
                //Even while we are pre-fetching the next page we can now yield and return the current results to the consumer for processing
                //  creating a true async paginated-stream of async enumerable data with improved performance!
                yield return currentPage;

            } while (iterationResponseTask != null);
        }
    }
}

#endif
