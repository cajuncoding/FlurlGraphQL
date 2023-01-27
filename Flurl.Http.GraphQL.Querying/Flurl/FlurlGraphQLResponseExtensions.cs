using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace Flurl.Http.GraphQL.Querying
{
    public static class FlurlGraphQLResponseExtensions
    {
        public static async Task<GraphQLQueryResults<TResult>> ReceiveGraphQLQueryResults<TResult>(this Task<IFlurlResponse> responseTask, string queryOperationName = null)
             where TResult : class
        {
            return await responseTask.ProcessResponsePayloadInternalAsync(resultPayload =>
            {
                //BBernard
                //Extract the Collection Data specified... or first data...
                //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
                var queryResultJson = resultPayload.Data;

                var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                    ? queryResultJson.First
                    : queryResultJson.Field(queryOperationName);

                var result = querySingleResultJson.ConvertToGraphQLResultsInternal<TResult>();
                return result;

            }).ConfigureAwait(false);
        }

        public static async Task<GraphQLQueryConnectionResult<TResult>> ReceiveGraphQLQueryConnectionResults<TResult>(this Task<IFlurlResponse> responseTask, string queryOperationName = null)
            where TResult : class
        {
            var graphqlResults = await responseTask.ReceiveGraphQLQueryResults<TResult>(queryOperationName).ConfigureAwait(false);
            return graphqlResults as GraphQLQueryConnectionResult<TResult>;
        }

        public static async Task<GraphQLBatchQueryResults> ReceiveGraphQLBatchQueryResults(this Task<IFlurlResponse> responseTask)
        {
            return await responseTask.ProcessResponsePayloadInternalAsync(resultPayload =>
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

        internal static async Task<TGraphQLResult> ProcessResponsePayloadInternalAsync<TGraphQLResult>(
            this Task<IFlurlResponse> responseTask, 
            Func<FlurlGraphQLResponsePayload, TGraphQLResult> payloadHandlerFunc
        )
        {
            using (var response = await responseTask.ConfigureAwait(false) as FlurlGraphQLResponse)
            {
                if (response == null) return default;

                var resultPayload = await response.GetJsonAsync<FlurlGraphQLResponsePayload>().ConfigureAwait(false);
                if (resultPayload == null)
                    throw new FlurlGraphQLException(
                        message: "The response from GraphQL is null and/or cannot be parsed as Json.",
                        graphqlQuery: response.GraphQLQuery,
                        httpStatusCode: (HttpStatusCode)response.StatusCode
                    );


                //Raise an Exception if any errors are returned...
                if (resultPayload?.Errors?.Any() ?? false)
                {
                    var responseContent = await response.GetStringAsync().ConfigureAwait(false);
                    throw new FlurlGraphQLException(resultPayload.Errors, response.GraphQLQuery, responseContent, (HttpStatusCode)response.StatusCode);
                }

                return payloadHandlerFunc.Invoke(resultPayload);
            }
        }

        internal static GraphQLQueryResults<TResult> ConvertToGraphQLResultsInternal<TResult>(this JToken json)
            where TResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TResult>();

            //Dynamically parse the data from the results...
            //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
            //          & Offset Paging model is a subset of Cursor Paging (less flexible).
            var pageInfo = json.Field(GraphQLFields.PageInfo)?.ToObject<GraphQLCursorPageInfo>();
            var totalCount = (int?)json.Field(GraphQLFields.TotalCount);

            List<TResult> entityResults = null;

            //Dynamically resolve the Nodes from either:
            // - the Nodes child of the Data Result (for nodes{} based queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            if (json.Field(GraphQLFields.Nodes) is JArray nodes)
            {
                entityResults = nodes.ToObject<List<TResult>>();
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json.Field(GraphQLFields.Edges) is JArray edges)
            {
                var entityType = typeof(TResult);

                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
                if (entityType.IsDerivedFromGenericParent(typeof(GraphQLEdge<>)))
                {
                    //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
                    entityResults = edges.Select(edge => edge?.ToObject<TResult>()).ToList();
                }
                //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
                else
                {
                    entityResults = edges.OfType<JObject>().Select(edge =>
                    {
                        var entityEdge = edge.Field(GraphQLFields.Node)?.ToObject<TResult>();

                        //If the entity implements IGraphQLEdge (e.g. the entity has a Cursor Property), then we can specify the Cursor...
                        if (entityEdge is IGraphQLEdge cursorEdge)
                            cursorEdge.Cursor = (string)edge.Field(GraphQLFields.Cursor);

                        return entityEdge;
                    }).ToList();

                }
            }
            else if (json is JArray results)
            {
                entityResults = results.ToObject<List<TResult>>();
            }

            if (totalCount != null || pageInfo != null)
                return new GraphQLQueryConnectionResult<TResult>(entityResults, totalCount, pageInfo);
            else
                return new GraphQLQueryResults<TResult>(entityResults);
        }

    }
}
