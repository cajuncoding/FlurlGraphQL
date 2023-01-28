using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace Flurl.Http.GraphQL.Querying
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        public static async Task<IGraphQLQueryResults<TResult>> ReceiveGraphQLQueryResults<TResult>(this Task<IFlurlResponse> responseTask, string queryOperationName = null)
             where TResult : class
        {
            return await responseTask.ProcessResponsePayloadInternalAsync((resultPayload, _) =>
            {
                var results = resultPayload.LoadTypedResults<TResult>();
                return results;

            }).ConfigureAwait(false);
        }

        public static async Task<IGraphQLQueryConnectionResult<TResult>> ReceiveGraphQLQueryConnectionResults<TResult>(this Task<IFlurlResponse> responseTask, string queryOperationName = null)
            where TResult : class
        {
            var graphqlResults = await responseTask.ReceiveGraphQLQueryResults<TResult>(queryOperationName).ConfigureAwait(false);
            return graphqlResults as IGraphQLQueryConnectionResult<TResult>;
        }

        /// <summary>
        /// Receives all data paginated as an IEnumerable that you can iterate over each resulting Page.
        /// This uses the formalized Relay spec for Connection pagination: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseTask"></param>
        /// <param name="queryOperationName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IList<IGraphQLQueryConnectionResult<TResult>>> ReceiveAllGraphQLQueryConnectionPages<TResult>(
            this Task<IFlurlResponse> responseTask, 
            string queryOperationName = null, 
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var pageResultsList = new List<IGraphQLQueryConnectionResult<TResult>>();
            Task<IFlurlResponse> iterationResponseTask = responseTask;
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

        public static async Task<IGraphQLBatchQueryResults> ReceiveGraphQLBatchQueryResults(this Task<IFlurlResponse> responseTask)
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
