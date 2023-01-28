﻿using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Flurl.Http.GraphQL.Querying
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
        {
            return await responseTask.ProcessResponsePayloadInternalAsync((resultPayload, _) =>
            {
                var results = resultPayload.LoadTypedResults<TResult>();
                return results;

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into the typed results along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <returns>Returns an IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IGraphQLQueryConnectionResult<TResult>> ReceiveGraphQLQueryConnectionResults<TResult>(this Task<IFlurlGraphQLResponse> responseTask, string queryOperationName = null)
            where TResult : class
        {
            var graphqlResults = await responseTask.ReceiveGraphQLQueryResults<TResult>(queryOperationName).ConfigureAwait(false);
            return graphqlResults as IGraphQLQueryConnectionResult<TResult>;
        }

        /// <summary>
        /// This will automatically iterate to retrieve ALL possible page results using the GraphQL query. It will return a list of pages containing the typed results
        /// along with associated cursor paging details as defined in the GraphQL Spec for Connections.
        /// The GraphQL query MUST support the (after: $after) variable, and return pageInfo.hasNextPage & pageInfo.endCursor in the results!
        /// This query will block and load all data into memory. If you are looking to stream data you should use the ReceiveGraphQLQueryConnectionPagesAsyncEnumerable()
        /// method that returns an IAsyncEnumerable to support streaming; but it is only available in .NET Standard 2.1 and later.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a List of ALL IGraphQLQueryConnectionResult set of typed results along with paging information returned by the query.</returns>
        public static async Task<IList<IGraphQLQueryConnectionResult<TResult>>> ReceiveAllGraphQLQueryConnectionPages<TResult>(
            this Task<IFlurlGraphQLResponse> responseTask, 
            string queryOperationName = null, 
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var pageResultsList = new List<IGraphQLQueryConnectionResult<TResult>>();
            Task<IFlurlGraphQLResponse> iterationResponseTask = responseTask;
            //Track our EndCursor to prevent infinite loops due to incorrect query; will be validated.
            string priorEndCursor = null;

            do
            {
                await iterationResponseTask.ProcessResponsePayloadInternalAsync((responsePayload, flurlGraphQLResponse) =>
                {
                    var originalGraphQLRequest = flurlGraphQLResponse.OriginalGraphQLRequest;

                    var pageResult = responsePayload.LoadTypedResults<TResult>() as IGraphQLQueryConnectionResult<TResult>;

                    //Validate the Page to see if we are able to continue our iteration...
                    var (hasNextPage, endCursor) = AssertCursorPageIsValidForEnumeration(pageResult?.PageInfo, responsePayload, flurlGraphQLResponse, priorEndCursor);
                    
                    //Update our tracking endCursor for validation...
                    priorEndCursor = endCursor;

                    //THIS Page is Good so we add it to our Results...
                    pageResultsList.Add(pageResult);

                    //If there is another page then Update our Variables and request the NEXT Page;
                    //  otherwise set our iteration to null to stop processing!
                    iterationResponseTask = !hasNextPage
                        ? null
                        : originalGraphQLRequest
                            .SetGraphQLVariable(GraphQLArgs.After, endCursor)
                            .PostGraphQLQueryAsync(cancellationToken);

                    //Since this is a Func we must return a value.
                    return pageResult;
                }).ConfigureAwait(false);

            } while (iterationResponseTask != null);

            return pageResultsList;
        }

        /// <summary>
        /// Processes/parses the results of multiple GraphQL queries, executed as a single request batch, into the typed results that can then be retrieved
        /// by the query index or query operation Name (case insensitive). Each query is unique in that it likely returns different Types, may be
        /// paginated, etc. The IGraphQLBatchQueryResults provides methods to then retrieve and parse the results of each query as needed.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLBatchQueryResults container that allows retrieval and handling of each query by it's index or operation name.</returns>
        public static async Task<IGraphQLBatchQueryResults> ReceiveGraphQLBatchQueryResults(this Task<IFlurlGraphQLResponse> responseTask)
        {
            return await responseTask.ProcessResponsePayloadInternalAsync((resultPayload, _) =>
            {
                //BBernard
                //Extract the Collection Data specified... or first data...
                //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
                var queryResultJson = resultPayload.Data;

                var operationResults = new List<GraphQLQueryOperationResult>();
                foreach (var prop in queryResultJson.Properties())
                    operationResults.Add(new GraphQLQueryOperationResult(prop.Name, prop.Value as JObject));

                var batchResults = new GraphQLBatchQueryResults(operationResults);
                return batchResults;
            }).ConfigureAwait(false);
        }
    }
}
