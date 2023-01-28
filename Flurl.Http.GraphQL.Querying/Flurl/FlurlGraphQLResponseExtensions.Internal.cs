using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Flurl.Util;

namespace Flurl.Http.GraphQL.Querying
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        internal static (bool HasNextPage, string EndCursor) AssertCursorPageIsValidForEnumeration(IGraphQLCursorPageInfo pageInfo, FlurlGraphQLResponsePayload responsePayload, FlurlGraphQLResponse flurlGraphQLResponse, string priorEndCursor)
        {
            if (pageInfo == null)
            {
                ThrowGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo node is missing. Check that the query is correct and that it correctly returns pageInfo.hasNextPage & pageInfo.endCursor values.");
            }

            bool? hasNextPageFlag = pageInfo.HasNextPage;
            string endCursor = pageInfo.EndCursor;

            if (hasNextPageFlag == null || endCursor == null)
            {
                ThrowGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.hasNextPage and/or the pageInfo.endCursor values are not available in the GraphQL query response.");
            }
            else if (endCursor == priorEndCursor)
            {
                ThrowGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.endCursor is returning the same value. Check that the query is correct and that it correctly implements the (after:$after) variable.");
            }
            
            //WE Know that HasNextPage has a value, but to make intellisense happy we check it here (redundantly)...
            return (hasNextPageFlag.HasValue && hasNextPageFlag.Value, endCursor);
        }

        internal static void ThrowGraphQLException(FlurlGraphQLResponsePayload responsePayload, FlurlGraphQLResponse flurlGraphQLResponse, string message)
            => throw new FlurlGraphQLException(message, flurlGraphQLResponse.GraphQLQuery, responsePayload, (HttpStatusCode)flurlGraphQLResponse.StatusCode);

        internal static async Task<TGraphQLResult> ProcessResponsePayloadInternalAsync<TGraphQLResult>(
            this Task<IFlurlResponse> responseTask, 
            Func<FlurlGraphQLResponsePayload, FlurlGraphQLResponse, TGraphQLResult> payloadHandlerFunc
        )
        {
            using (var response = await responseTask.ConfigureAwait(false) as FlurlGraphQLResponse)
            {
                if (response == null) return default;

                var resultPayload = await response.GetJsonAsync<FlurlGraphQLResponsePayload>().ConfigureAwait(false);
                //Raise an Exception if null or if any errors are returned...
                if (resultPayload == null)
                {
                    throw new FlurlGraphQLException(graphqlQuery: response.GraphQLQuery, httpStatusCode: (HttpStatusCode)response.StatusCode,
                        message: "The response from GraphQL is null and/or cannot be parsed as Json.");
                }
                else if (resultPayload.Errors?.Any() ?? false)
                {
                    var responseContent = await response.GetStringAsync().ConfigureAwait(false);
                    throw new FlurlGraphQLException(resultPayload.Errors, response.GraphQLQuery, responseContent, (HttpStatusCode)response.StatusCode);
                }

                return payloadHandlerFunc.Invoke(resultPayload, response);
            }
        }

        internal static IGraphQLQueryResults<TResult> ConvertToGraphQLResultsInternal<TResult>(this JToken json)
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
