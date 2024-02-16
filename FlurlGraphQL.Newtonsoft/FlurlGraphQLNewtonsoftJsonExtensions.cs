using Flurl.Http;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.TypeCacheHelpers;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL
{
    public static class FlurlGraphQLNewtonsoftJsonExtensions
    {
        #region Configuration Extension - NewtonsoftJson Serializer Settings (ONLY Available after an IFlurlRequest is initialized)...

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="jsonSerializerSettings"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLNewtonsoftJsonSerializerSettings(this IFlurlRequest request, JsonSerializerSettings jsonSerializerSettings)
        {
            jsonSerializerSettings.AssertArgIsNotNull(nameof(jsonSerializerSettings));

            var graphqlRequest = (FlurlGraphQLRequest)request.ToGraphQLRequest();
            graphqlRequest.SetContextItem(ContextItemKeys.NewtonsoftJsonSerializerSettings, jsonSerializerSettings);

            return graphqlRequest;
        }

        #endregion

        #region FlurlGraphQLResponse Extensions for Newtonsoft Json...

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw JObject (Newtonsoft.Json) Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<JObject> ReceiveGraphQLRawJsonResponseJObject(this Task<IFlurlGraphQLResponse> responseTask)
            => (await responseTask.ReceiveGraphQLRawJsonResponse().ConfigureAwait(false)) as JObject;

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw JObject (Newtonsoft.Json) Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<JObject> ReceiveGraphQLRawJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawJsonResponseJObject();

        #endregion

        #region Json Parsing Extensions

        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsInternal<TEntityResult>(this JToken json, JsonSerializerSettings jsonSerializerSettings = null)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var jsonSerializer = JsonSerializer.CreateDefault(jsonSerializerSettings);
            jsonSerializer.Converters.Add(new GraphQLPageResultsToICollectionConverter());

            return ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(json, jsonSerializer);
        }

        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(this JToken json, JsonSerializer jsonSerializer)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

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
            if (json.Field(GraphQLFields.Nodes) is JArray nodesJson)
            {
                entityResults = nodesJson.ToObject<List<TEntityResult>>(jsonSerializer);
                paginationType = PaginationType.Cursor;
            }
            else if (json.Field(GraphQLFields.Items) is JArray itemsJson)
            {
                entityResults = itemsJson.ToObject<List<TEntityResult>>(jsonSerializer);
                paginationType = PaginationType.Offset;
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json.Field(GraphQLFields.Edges) is JArray edgesJson)
            {
                paginationType = PaginationType.Cursor;
                var entityType = typeof(TEntityResult);

                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
                if (entityType.IsDerivedFromGenericParent(GraphQLTypeCache.CachedIGraphQLEdgeGenericType))
                {
                    //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
                    //entityResults = edges.Select(edge => edge?.ToObject<TEntityResult>(jsonSerializer)).ToList();
                    entityResults = edgesJson.ToObject<List<TEntityResult>>(jsonSerializer);
                }
                //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
                else
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .ToObject<List<TEntityResult>>(jsonSerializer);
                }
            }
            else
            {
                switch (json)
                {
                    case JArray arrayResults:
                        entityResults = arrayResults.ToObject<List<TEntityResult>>(jsonSerializer);
                        break;
                    // ReSharper disable once MergeIntoPattern
                    case JObject jsonObj when jsonObj.First is JArray firstArrayResults:
                        entityResults = firstArrayResults.ToObject<List<TEntityResult>>(jsonSerializer);
                        break;
                    //If only a single Object was returned then this is likely a Mutation so we return the single
                    //  item as the first-and-only result of the set...
                    case JObject jsonObj:
                        var singleResult = jsonObj.ToObject<TEntityResult>(jsonSerializer);
                        entityResults = new List<TEntityResult>() { singleResult };
                        break;
                }
            }

            //If the results have Paging Info we map to the correct type (Connection/Cursor or CollectionSegment/Offset)...
            if (paginationType == PaginationType.Cursor)
                return new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo);
            else if (paginationType == PaginationType.Offset)
                return new GraphQLCollectionSegmentResults<TEntityResult>(entityResults, totalCount, GraphQLOffsetPageInfo.FromCursorPageInfo(pageInfo));
            //If we have a Total Count then we also must return a Paging result because it's possible to request TotalCount by itself without any other PageInfo or Nodes...
            //NOTE: WE must check this AFTER we process based on Cursor Type to make sure Cursor/Offset are both handled (if specified)...
            else if (totalCount.HasValue)
                return new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo);
            //If not a paging result then we simply return the typed results...
            else
                return new GraphQLQueryResults<TEntityResult>(entityResults);
        }

        internal static JArray FlattenGraphQLEdgesJsonToArrayOfNodes(this JArray edgesJson)
        {
            var edgeNodesEnumerable = edgesJson
                .OfType<JObject>()
                .Select(edge =>
                {
                    var node = edge.Field(GraphQLFields.Node) as JObject;

                    //If not already defined, we map the Edges Cursor value to the Node so that the model is simplified
                    //  and any consumer can just as a "Cursor" property to their model to get the node's cursor.
                    if (node != null && node.Field(GraphQLFields.Cursor) == null)
                        node.Add(GraphQLFields.Cursor, edge.Field(GraphQLFields.Cursor));

                    return node;
                })
                .Where(n => n != null && n.Type != JTokenType.Null);

            var edgeNodesJson = new JArray(edgeNodesEnumerable);
            return edgeNodesJson;
        }

        #endregion
    }
}