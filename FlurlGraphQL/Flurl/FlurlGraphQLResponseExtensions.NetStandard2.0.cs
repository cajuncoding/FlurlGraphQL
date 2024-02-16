using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

#if NETSTANDARD2_0 || NET461

namespace FlurlGraphQL
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        /// <summary>
        /// This will automatically enumerate through ALL possible Connection/Cursor page results, using the GraphQL query, as a series of IEnumerable Tasks that can then be awaited.
        /// This offers a form of async streaming in netstandard2.0 in that while you handle one page another async Task pre-fetching the next page asynchronously is initialized.
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
        public static IEnumerable<Task<IGraphQLConnectionResults<TResult>>> ReceiveGraphQLConnectionPagesAsEnumerableTasks<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            Task<IFlurlGraphQLResponse> iterationResponseTask = responseTask;
            //Track our EndCursor to prevent infinite loops due to incorrect query; will be validated.
            string priorEndCursor = null;

            do
            {
                var currentPageTask = iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    IGraphQLConnectionResults<TResult> pageResult;
                    (pageResult, priorEndCursor,iterationResponseTask) = ProcessPayloadIterationForCursorPaginationAsyncEnumeration<TResult>(
                        queryOperationName, 
                        priorEndCursor, 
                        responsePayload, 
                        flurlGraphQLResponse, 
                        cancellationToken
                    );

                    return pageResult;
                });

                //BBernard
                //Even though nothing has been awaited yet (so not fully streaming) we are using parallel async tasks to pre-fetch the next page as
                //  as fast as possible even before we yield and return the current results to the consumer for processing
                //  creating a pseudo-paginated-stream of enumerable awaitable async tasks containing the data with improved performance!
                yield return currentPageTask;

            } while (iterationResponseTask != null);
        }

        /// <summary>
        /// This will automatically enumerate through ALL possible CollectionSegment/Offset page results, using the GraphQL query, as a series of IEnumerable Tasks that can then be awaited.
        /// This offers a form of async streaming in netstandard2.0 in that while you handle one page another async Task pre-fetching the next page asynchronously is initialized.
        /// This is great for streaming data from GraphQL to another destination (e.g. a Database).
        /// The GraphQL query MUST support the (skip: $skip) variable, and return pageInfo.hasNextPage in the results for Offset pagination!
        /// This assumes that the query uses Offset Pagination on a GraphQL CollectionSegment operation compatible with the Offset Paging specification defined by the
        /// HotChocolate GraphQL Server for .NET.
        /// See: https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns an IAsyncEnumerable of IGraphQLQueryConnectionResult sets containing the typed results along with paging information returned by the query.</returns>
        public static IEnumerable<Task<IGraphQLCollectionSegmentResults<TResult>>> ReceiveGraphQLCollectionSegmentPagesAsEnumerableTasks<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            Task<IFlurlGraphQLResponse> iterationResponseTask = responseTask;

            do
            {
                var currentPageTask = iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    IGraphQLCollectionSegmentResults<TResult> pageResult;
                    (pageResult, iterationResponseTask) = ProcessPayloadIterationForOffsetPaginationAsyncEnumeration<TResult>(
                        queryOperationName,
                        responsePayload,
                        flurlGraphQLResponse,
                        cancellationToken
                    );

                    return pageResult;
                });

                //BBernard
                //Even though nothing has been awaited yet (so not fully streaming) we are using parallel async tasks to pre-fetch the next page as
                //  as fast as possible even before we yield and return the current results to the consumer for processing
                //  creating a pseudo-paginated-stream of enumerable awaitable async tasks containing the data with improved performance!
                yield return currentPageTask;

            } while (iterationResponseTask != null);
        }
    }
}

#endif
