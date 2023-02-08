using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Flurl.Http.GraphQL.Querying.NewtonsoftJson;
using Newtonsoft.Json;

namespace Flurl.Http.GraphQL.Querying
{
    public static partial class FlurlGraphQLResponseExtensions
    {
        internal enum PaginationType
        {
            Cursor,
            Offset
        };

        internal static (bool HasNextPage, string EndCursor) AssertCursorPageIsValidForEnumeration(IGraphQLCursorPageInfo pageInfo, FlurlGraphQLResponsePayload responsePayload, FlurlGraphQLResponse flurlGraphQLResponse, string priorEndCursor)
        {
            if (pageInfo == null)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo node is missing. Check that the query is correct and that it correctly returns pageInfo.hasNextPage & pageInfo.endCursor values for Cursor based paging.");
            }

            bool? hasNextPageFlag = pageInfo.HasNextPage;
            string endCursor = pageInfo.EndCursor;

            if (hasNextPageFlag == null || endCursor == null)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.hasNextPage and/or the pageInfo.endCursor values are not available in the GraphQL query response.");
            }
            else if (endCursor == priorEndCursor)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.endCursor is returning the same value. Check that the query is correct and that it correctly implements the (after:$after) variable.");
            }

            return (hasNextPageFlag.Value, endCursor);
        }

        internal static bool AssertOffsetPageIsValidForEnumeration(IGraphQLOffsetPageInfo pageInfo, FlurlGraphQLResponsePayload responsePayload, FlurlGraphQLResponse flurlGraphQLResponse, int skipVariable)
        {
            if (skipVariable <= 0)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the skip variable is missing. Check that the query is correct and that it correctly implements the (skip: $skip) variable for Offset based paging.");
            }

            if (pageInfo == null)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo node is missing. Check that the query is correct and that it correctly returns pageInfo.hasNextPage value for Offset based paging.");
            }

            bool? hasNextPageFlag = pageInfo?.HasNextPage;
            if (hasNextPageFlag == null)
            {
                throw NewGraphQLException(responsePayload, flurlGraphQLResponse,
                    "Unable to enumerate all pages because the pageInfo.hasNextPage value is not available in the GraphQL query response.");
            }

            return hasNextPageFlag.Value;
        }

        internal static FlurlGraphQLException NewGraphQLException(FlurlGraphQLResponsePayload responsePayload, FlurlGraphQLResponse flurlGraphQLResponse, string message)
            => new FlurlGraphQLException(message, flurlGraphQLResponse.GraphQLQuery, responsePayload, (HttpStatusCode)flurlGraphQLResponse.StatusCode);

        /// <summary>
        /// Internal handler to process the payload in Cursor Pagination Async Enumeration methods... this method supports both:
        ///     - netstandard2.0 IEnumerable&lt;Task&lt;?&gt;&gt; for async enumeration of pages one-by-one (legacy)
        ///     - netstandard2.1+ AsyncEnumerable&lt;?&gt; for true async streaming of pages where the next Page is pre-fetched while yielding the current page.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryOperationName"></param>
        /// <param name="priorEndCursor"></param>
        /// <param name="responsePayload"></param>
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
            FlurlGraphQLResponsePayload responsePayload,
            FlurlGraphQLResponse flurlGraphQLResponse, 
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var pageResult = responsePayload.LoadTypedResults<TResult>(queryOperationName) as IGraphQLConnectionResults<TResult>;

            //Validate the Page to see if we are able to continue our iteration...
            var (hasNextPage, endCursor) = AssertCursorPageIsValidForEnumeration(pageResult?.PageInfo, responsePayload, flurlGraphQLResponse, priorEndCursor);

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
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryOperationName"></param>
        /// <param name="priorEndCursor"></param>
        /// <param name="responsePayload"></param>
        /// <param name="flurlGraphQLResponse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static (
            IGraphQLCollectionSegmentResults<TResult> PageResult,
            Task<IFlurlGraphQLResponse> NextIterationResponseTask
        ) ProcessPayloadIterationForOffsetPaginationAsyncEnumeration<TResult>(
            string queryOperationName,
            FlurlGraphQLResponsePayload responsePayload,
            FlurlGraphQLResponse flurlGraphQLResponse,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            var originalGraphQLRequest = flurlGraphQLResponse.GraphQLRequest;
            var pageResult = responsePayload.LoadTypedResults<TResult>(queryOperationName) as IGraphQLCollectionSegmentResults<TResult>;

            //Get the current Skip Variable so that we can dynamically increment it to continue the pagination!
            var currentSkipVariable = originalGraphQLRequest.GetGraphQLVariable(GraphQLCollectionSegmentArgs.Skip) as int? ?? 0;
            
            //Detect if we are safely enumerating and encountered the end of the results
            //NOTE: We must check this before our validation to prevent exceptions for otherwise valid end of results;
            //      in which case we stop the iteration by returning null NextIterationResponseTask along with the null results.
            if (currentSkipVariable > 0 && pageResult != null && !pageResult.HasAnyResults())
                return (null, null);

            var nextSkipVariable = (currentSkipVariable + pageResult?.Count ?? 0);

            //Validate the Page to see if we are able to continue our iteration...
            var hasNextPage = AssertOffsetPageIsValidForEnumeration(pageResult?.PageInfo, responsePayload, flurlGraphQLResponse, nextSkipVariable);

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

        internal static async Task<TGraphQLResult> ProcessResponsePayloadInternalAsync<TGraphQLResult>(
            this Task<IFlurlGraphQLResponse> responseTask, 
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

        private static readonly Type CachedIGraphQLEdgeGenericType = typeof(IGraphQLEdge<>);
        
        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsInternal<TEntityResult>(this JToken json)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var jsonSerializer = JsonSerializer.CreateDefault();
            //jsonSerializer.ContractResolver = GraphQLAdaptiveJsonContractResolver.Instance;
            jsonSerializer.Converters.Add(new GraphQLPaginatedResultsJsonConverter());

            //Dynamically parse the data from the results...
            //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
            //          & Offset Paging model is a subset of Cursor Paging (less flexible).
            var pageInfo = json.Field(GraphQLFields.PageInfo)?.ToObject<GraphQLCursorPageInfo>();
            var totalCount = (int?)json.Field(GraphQLFields.TotalCount);

            PaginationType? paginationType = null;
            List<TEntityResult> entityResults = null;

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            if (json.Field(GraphQLFields.Nodes) is JArray nodes)
            {
                entityResults = nodes.ToObject<List<TEntityResult>>(jsonSerializer);
                paginationType = PaginationType.Cursor;
            }
            else if (json.Field(GraphQLFields.Items) is JArray items)
            {
                entityResults = items.ToObject<List<TEntityResult>>(jsonSerializer);
                paginationType = PaginationType.Offset;
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json.Field(GraphQLFields.Edges) is JArray edges)
            {
                paginationType = PaginationType.Cursor;
                var entityType = typeof(TEntityResult);

                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
                if (entityType.IsDerivedFromGenericParent(CachedIGraphQLEdgeGenericType))
                {
                    //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
                    //entityResults = edges.Select(edge => edge?.ToObject<TEntityResult>(jsonSerializer)).ToList();
                    entityResults = edges.ToObject<List<TEntityResult>>(jsonSerializer);
                }
                //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
                else
                {
                    entityResults = edges.OfType<JObject>().Select(edge =>
                    {
                        var entityEdge = edge.Field(GraphQLFields.Node)?.ToObject<TEntityResult>(jsonSerializer);

                        //If the entity implements IGraphQLEdge (e.g. the entity has a Cursor Property), then we can specify the Cursor...
                        if (entityEdge is IGraphQLEdge cursorEdge)
                            cursorEdge.Cursor = (string)edge.Field(GraphQLFields.Cursor);

                        return entityEdge;
                    }).ToList();
                }
            }
            else switch (json)
            {
                case JArray arrayResults:
                    entityResults = arrayResults.ToObject<List<TEntityResult>>(jsonSerializer);
                    break;
                case JObject jsonObj when jsonObj.First is JArray firstArrayResults:
                    entityResults = firstArrayResults.ToObject<List<TEntityResult>>(jsonSerializer);
                    break;
            }

            //If the results have Paging Info we map to the correct type (Connection/Cursor or CollectionSegment/Offset)...
            //NOTE: If we have a Total Count then we also must return a Paging result because it's possible to
            //      request TotalCount by itself without any other PageInfo or Nodes...
            if (paginationType == PaginationType.Cursor || totalCount.HasValue)
            {
                return new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo);
            }
            else if (paginationType == PaginationType.Offset)
            {
                return new GraphQLCollectionSegmentResults<TEntityResult>(entityResults, totalCount, GraphQLOffsetPageInfo.FromCursorPageInfo(pageInfo));
            }

            //If not a paging result then we simply return the typed results...
            return new GraphQLQueryResults<TEntityResult>(entityResults);
        }

    }
}
