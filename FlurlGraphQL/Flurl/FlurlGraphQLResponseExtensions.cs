using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Text.Json;

namespace FlurlGraphQL
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a simple set of results ready for processing.
        /// This assumes that the Query did not include paging details or other extension data such as total count, etc.
        /// This will return the results of the first query if more than one are specified unless an operationName is provided which will then get those results.
        /// However, if you are executing multiple queries then you should likely be using the ReceiveGraphQLBatchQueryResults() instead.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<IGraphQLQueryResults<TResult>> ReceiveGraphQLQueryResults<TResult>(this Task<IFlurlGraphQLResponse> responseTask, string queryOperationName = null)
            where TResult : class
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _) => responseProcessor.LoadTypedResults<TResult>(queryOperationName)).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a simple set of results ready for processing.
        /// This assumes that the Query did not include paging details or other extension data such as total count, etc.
        /// This will return the results of the first query if more than one are specified unless an operationName is provided which will then get those results.
        /// However, if you are executing multiple queries then you should likely be using the ReceiveGraphQLBatchQueryResults() instead.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<IGraphQLQueryResults<TResult>> ReceiveGraphQLQueryResults<TResult>(this IFlurlGraphQLResponse response, string queryOperationName = null)
            where TResult : class => Task.FromResult(response).ReceiveGraphQLQueryResults<TResult>(queryOperationName);

        /// <summary>
        /// Processes/parses the results of the GraphQL mutation execution into the specified result payload. 
        /// This assumes that the Mutation conventions used follow best practices in that GraphQL mutations should take in and Input payload
        /// and return a result Payload; the result payload may be the single object type or a root type for a collection of results & errors (as defined by the GraphQL Schema).
        /// However, if you need more control over the processing of the Results you can dynamically handle it with the ReceiveGraphQLRawJsonResponse() method.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<TResult> ReceiveGraphQLMutationResult<TResult>(this Task<IFlurlGraphQLResponse> responseTask, string queryOperationName = null)
            where TResult : class
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _)
                //For Single Item Mutation Result, we process with the same logic but there will be only one item (vs many for a Query)...
                => responseProcessor.LoadTypedResults<TResult>(queryOperationName)?.FirstOrDefault()).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of the GraphQL mutation execution into the specified result payload. 
        /// This assumes that the Mutation conventions used follow best practices in that GraphQL mutations should take in and Input payload
        /// and return a result Payload; the result payload may be the single object type or a root type for a collection of results & errors (as defined by the GraphQL Schema).
        /// However, if you need more control over the processing of the Results you can dynamically handle it with the ReceiveGraphQLRawJsonResponse() method.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<TResult> ReceiveGraphQLMutationResult<TResult>(this IFlurlGraphQLResponse response, string queryOperationName = null)
            where TResult : class => Task.FromResult(response).ReceiveGraphQLMutationResult<TResult>(queryOperationName);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// The result of this method may be a 'JsonObject' if using System.Text.Json serialization with Flurl or a 'JObject' if using Newtonsoft Json Serialization.
        /// There are additional convenience methods available to handle the casting for you via ReceiveGraphQLRawJsonResponseJsonNode() or ReceiveGraphQLRawJsonResponseJObject() respectively.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<object> ReceiveGraphQLRawJsonResponse(this Task<IFlurlGraphQLResponse> responseTask)
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _) => responseProcessor.Data).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<object> ReceiveGraphQLRawJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawJsonResponse();

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw JsonElement (System.Text.Json) Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<JsonNode> ReceiveGraphQLRawJsonResponseJsonNode(this Task<IFlurlGraphQLResponse> responseTask)
            => (JsonNode)(await responseTask.ReceiveGraphQLRawJsonResponse().ConfigureAwait(false));

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw JsonElement (System.Text.Json) Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<JsonNode> ReceiveGraphQLRawJsonResponseJsonNode(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawJsonResponseJsonNode();

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into the typed results along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IGraphQLConnectionResults<TResult>> ReceiveGraphQLConnectionResults<TResult>(this Task<IFlurlGraphQLResponse> responseTask, string queryOperationName = null)
            where TResult : class
        {
            var graphqlResults = await responseTask.ReceiveGraphQLQueryResults<TResult>(queryOperationName).ConfigureAwait(false);
            return graphqlResults.ToGraphQLConnectionResultsInternal();
        }

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into the typed results along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static Task<IGraphQLConnectionResults<TResult>> ReceiveGraphQLConnectionResults<TResult>(this IFlurlGraphQLResponse response, string queryOperationName = null)
            where TResult : class => Task.FromResult(response).ReceiveGraphQLConnectionResults<TResult>(queryOperationName);

        /// <summary>
        /// This will automatically iterate to retrieve ALL possible page results using the GraphQL query. It will return a list of pages containing the typed results
        /// along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// The GraphQL query MUST support the (after: $after) variable, and return pageInfo.hasNextPage & pageInfo.endCursor in the results!
        /// This query will block and load all data into memory. If you are looking to stream data you should use the ReceiveGraphQLQueryConnectionPagesAsyncEnumerable()
        /// method that returns an IAsyncEnumerable to support streaming; but it is only available in .NET Standard 2.1 and later.
        /// This assumes that the query uses Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a List of ALL IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IList<IGraphQLConnectionResults<TResult>>> ReceiveAllGraphQLQueryConnectionPages<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var pageResultsList = new List<IGraphQLConnectionResults<TResult>>();
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

                pageResultsList.Add(currentPage);

            } while (iterationResponseTask != null);

            return pageResultsList;
        }

        /// <summary>
        /// This will automatically iterate to retrieve ALL possible page results using the GraphQL query. It will return a list of pages containing the typed results
        /// along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// The GraphQL query MUST support the (after: $after) variable, and return pageInfo.hasNextPage & pageInfo.endCursor in the results!
        /// This query will block and load all data into memory. If you are looking to stream data you should use the ReceiveGraphQLQueryConnectionPagesAsyncEnumerable()
        /// method that returns an IAsyncEnumerable to support streaming; but it is only available in .NET Standard 2.1 and later.
        /// This assumes that the query uses Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a List of ALL IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static Task<IList<IGraphQLConnectionResults<TResult>>> ReceiveAllGraphQLQueryConnectionPages<TResult>(
            this IFlurlGraphQLResponse response,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class => Task.FromResult(response).ReceiveAllGraphQLQueryConnectionPages<TResult>(queryOperationName, cancellationToken);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into the typed results along with associated offset paging info
        /// as CollectionSegment details compatible with the HotChocolate GraphQL server Offset paging specification.
        /// This assumes that the query uses Offset Pagination on a GraphQL CollectionSegment operation compatible with the Offset Paging specification defined by the
        /// HotChocolate GraphQL Server for .NET.
        /// See: https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IGraphQLCollectionSegmentResults<TResult>> ReceiveGraphQLCollectionSegmentResults<TResult>(this Task<IFlurlGraphQLResponse> responseTask, string queryOperationName = null)
            where TResult : class
        {
            var graphqlResults = await responseTask.ReceiveGraphQLQueryResults<TResult>(queryOperationName).ConfigureAwait(false);
            return graphqlResults.ToGraphQLConnectionResultsInternal().ToCollectionSegmentResultsInternal();
        }

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into the typed results along with associated offset paging info
        /// as CollectionSegment details compatible with the HotChocolate GraphQL server Offset paging specification.
        /// This assumes that the query uses Offset Pagination on a GraphQL CollectionSegment operation compatible with the Offset Paging specification defined by the
        /// HotChocolate GraphQL Server for .NET.
        /// See: https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static Task<IGraphQLCollectionSegmentResults<TResult>> ReceiveGraphQLCollectionSegmentResults<TResult>(this IFlurlGraphQLResponse response, string queryOperationName = null)
            where TResult : class => Task.FromResult(response).ReceiveGraphQLCollectionSegmentResults<TResult>(queryOperationName);

        /// <summary>
        /// This will automatically iterate to retrieve ALL possible page results using the GraphQL query. It will return a list of pages containing the typed results
        /// along with associated offset paging info as CollectionSegment details compatible with the HotChocolate GraphQL server Offset paging specification.
        /// The GraphQL query MUST support the (skip: $skip) variable, and return pageInfo.hasNextPage in the results!
        /// This query will block and load all data into memory. If you are looking to stream data you should use the ReceiveGraphQLQueryCollectionSegmentPagesAsyncEnumerable()
        /// method that returns an IAsyncEnumerable to support streaming; but it is only available in .NET Standard 2.1 and later.
        /// This assumes that the query uses Offset Pagination on a GraphQL CollectionSegment operation compatible with the Offset Paging specification defined by the
        /// HotChocolate GraphQL Server for .NET.
        /// See: https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a List of ALL IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IList<IGraphQLCollectionSegmentResults<TResult>>> ReceiveAllGraphQLQueryCollectionSegmentPages<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var pageResultsList = new List<IGraphQLCollectionSegmentResults<TResult>>();
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

                //THIS Page is Good so we add it to our Results...
                pageResultsList.Add(currentPage);

            } while (iterationResponseTask != null);

            return pageResultsList;
        }

        /// <summary>
        /// This will automatically iterate to retrieve ALL possible page results using the GraphQL query. It will return a list of pages containing the typed results
        /// along with associated offset paging info as CollectionSegment details compatible with the HotChocolate GraphQL server Offset paging specification.
        /// The GraphQL query MUST support the (skip: $skip) variable, and return pageInfo.hasNextPage in the results!
        /// This query will block and load all data into memory. If you are looking to stream data you should use the ReceiveGraphQLQueryCollectionSegmentPagesAsyncEnumerable()
        /// method that returns an IAsyncEnumerable to support streaming; but it is only available in .NET Standard 2.1 and later.
        /// This assumes that the query uses Offset Pagination on a GraphQL CollectionSegment operation compatible with the Offset Paging specification defined by the
        /// HotChocolate GraphQL Server for .NET.
        /// See: https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a List of ALL IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static Task<IList<IGraphQLCollectionSegmentResults<TResult>>> ReceiveAllGraphQLQueryCollectionSegmentPages<TResult>(
            this IFlurlGraphQLResponse response,
            string queryOperationName = null,
            CancellationToken cancellationToken = default
        ) where TResult : class => Task.FromResult(response).ReceiveAllGraphQLQueryCollectionSegmentPages<TResult>(queryOperationName, cancellationToken);

        /// <summary>
        /// Processes/parses the results of multiple GraphQL queries, executed as a single request batch, into the typed results that can then be retrieved
        /// by the query index or query operation Name (case insensitive). Each query is unique in that it likely returns different Types, may be
        /// paginated, etc. The IGraphQLBatchQueryResults provides methods to then retrieve and parse the results of each query as needed.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLBatchQueryResults container that allows retrieval and handling of each query by it's index or operation name.</returns>
        public static async Task<IGraphQLBatchQueryResults> ReceiveGraphQLBatchQueryResults(this Task<IFlurlGraphQLResponse> responseTask)
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _) => responseProcessor.LoadBatchQueryResults()).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of multiple GraphQL queries, executed as a single request batch, into the typed results that can then be retrieved
        /// by the query index or query operation Name (case insensitive). Each query is unique in that it likely returns different Types, may be
        /// paginated, etc. The IGraphQLBatchQueryResults provides methods to then retrieve and parse the results of each query as needed.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLBatchQueryResults container that allows retrieval and handling of each query by it's index or operation name.</returns>
        public static Task<IGraphQLBatchQueryResults> ReceiveGraphQLBatchQueryResults(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLBatchQueryResults();
    }
}
