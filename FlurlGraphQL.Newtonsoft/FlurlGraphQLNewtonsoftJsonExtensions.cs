using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.TypeCacheHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.JsonProcessing
{
    public static class FlurlGraphQLNewtonsoftJsonExtensions
    {
        #region Add Dynamic support that is only available with Newtonsoft Json...

        public static Task<dynamic> GetJsonAsync(this IFlurlGraphQLResponse graphqlResponse)
            => graphqlResponse.AsFlurlGraphQLResponse()?.BaseFlurlResponse?.GetJsonAsync();

        public static Task<IList<dynamic>> GetJsonListAsync(this IFlurlGraphQLResponse graphqlResponse)
            => graphqlResponse.AsFlurlGraphQLResponse()?.BaseFlurlResponse?.GetJsonListAsync();

        private static FlurlGraphQLResponse AsFlurlGraphQLResponse(this IFlurlGraphQLResponse graphqlResponse)
            => (graphqlResponse as FlurlGraphQLResponse) ?? throw new ArgumentException($"The GraphQL Response is not of the expected type [{nameof(FlurlGraphQLResponse)}].", nameof(graphqlResponse));

        #endregion

        #region Configuration Extension - NewtonsoftJson Serializer Settings (ONLY Available after an IFlurlRequest is initialized)...

        /// <summary>
        /// Initialize a custom Json Serializer using Newtonsoft.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="newtonsoftJsonSettings"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLNewtonsoftJson(this IFlurlRequest request, JsonSerializerSettings newtonsoftJsonSettings = null)
            => request.ToGraphQLRequest().UseGraphQLNewtonsoftJson(newtonsoftJsonSettings);

        /// <summary>
        /// Initialize a custom GraphQL Json Serializer using Newtonsoft.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="newtonsoftJsonSettings"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLNewtonsoftJson(this IFlurlGraphQLRequest graphqlRequest, JsonSerializerSettings newtonsoftJsonSettings)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                if (newtonsoftJsonSettings == null && flurlGraphQLRequest.GraphQLJsonSerializer is IFlurlGraphQLNewtonsoftJsonSerializer existingNewtonsoftJsonSerializer)
                    flurlGraphQLRequest.GraphQLJsonSerializer = existingNewtonsoftJsonSerializer;
                else
                    flurlGraphQLRequest.GraphQLJsonSerializer = new FlurlGraphQLNewtonsoftJsonSerializer(
                        newtonsoftJsonSettings ?? FlurlGraphQLNewtonsoftJsonSerializer.CreateDefaultSerializerSettings()
                    );
            }

            return graphqlRequest;
        }

        #endregion

        #region Json Parsing Extensions - JsonConvert Strategy

        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsInternal<TEntityResult>(this JToken json, JsonSerializerSettings jsonSerializerSettings = null)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var jsonSerializer = JsonSerializer.CreateDefault(jsonSerializerSettings);
            jsonSerializer.Converters.Add(new FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter());

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
            IReadOnlyList<TEntityResult> entityResults = null;

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            if (json.Field(GraphQLFields.Nodes) is JArray nodesJson)
            {
                entityResults = nodesJson.ToObject<TEntityResult[]>(jsonSerializer);
                paginationType = PaginationType.Cursor;
            }
            else if (json.Field(GraphQLFields.Items) is JArray itemsJson)
            {
                entityResults = itemsJson.ToObject<TEntityResult[]>(jsonSerializer);
                paginationType = PaginationType.Offset;
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json.Field(GraphQLFields.Edges) is JArray edgesJson)
            {
                paginationType = PaginationType.Cursor;
                var entityType = typeof(TEntityResult);

                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
                if (entityType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLEdgeEntityType))
                {
                    //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
                    //entityResults = edges.Select(edge => edge?.ToObject<TEntityResult>(jsonSerializer)).ToList();
                    entityResults = edgesJson.ToObject<TEntityResult[]>(jsonSerializer);
                }
                //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
                else
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .ToObject<TEntityResult[]>(jsonSerializer);
                }
            }
            else
            {
                switch (json)
                {
                    case JArray arrayResults:
                        entityResults = arrayResults.ToObject<TEntityResult[]>(jsonSerializer);
                        break;
                    //TODO: Determine what this use case is really here to support????
                    case JObject jsonObj when jsonObj.First is JArray firstArrayResults:
                        entityResults = firstArrayResults.ToObject<TEntityResult[]>(jsonSerializer);
                        break;
                    //If only a single Object was returned then this is likely a Mutation so we return the single
                    //  item as the first-and-only result of the set...
                    case JObject jsonObj:
                        var singleResult = jsonObj.ToObject<TEntityResult>(jsonSerializer);
                        entityResults = new[] { singleResult };
                        break;
                }
            }

            switch (paginationType)
            {
                //If the results have Paging Info we map to the correct type (Connection/Cursor or CollectionSegment/Offset)...
                case PaginationType.Cursor:
                    return new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo);
                case PaginationType.Offset:
                    return new GraphQLCollectionSegmentResults<TEntityResult>(entityResults, totalCount, GraphQLOffsetPageInfo.FromCursorPageInfo(pageInfo));
                default:
                {
                    //If we have a Total Count then we also must return a Paging result because it's possible to request TotalCount by itself without any other PageInfo or Nodes...
                    return totalCount.HasValue 
                        ? new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo) 
                        : new GraphQLQueryResults<TEntityResult>(entityResults);
                }
            }
        }

        internal static JArray FlattenGraphQLEdgesJsonToArrayOfNodes(this JArray edgesJson)
        {
            var edgeNodesEnumerable = edgesJson
                .OfType<JObject>()
                .Select(edge =>
                {
                    var node = edge.Field(GraphQLFields.Node) as JObject;

                    //TODO: ADD Support to migrate ALL Edge values to the Node (similar to how System.Text.Json) now provides!
                    //If not already defined, we map the Edges Cursor value to the Node so that the model is simplified
                    //  and any consumer can just add a "Cursor" property to their model to get the node's cursor.
                    if (!node.IsNullOrUndefinedJson() && node.Field(GraphQLFields.Cursor) == null)
                        node.Add(GraphQLFields.Cursor, edge.Field(GraphQLFields.Cursor));

                    return node;
                })
                .Where(n => n != null && n.Type != JTokenType.Null);

            var edgeNodesJson = new JArray(edgeNodesEnumerable);
            return edgeNodesJson;
        }

        public static bool IsNullOrUndefinedJson(this JToken jsonToken)
            => jsonToken == null || jsonToken.Type == JTokenType.Null || jsonToken.Type == JTokenType.Undefined;

        #endregion

        #region Json Parsing Extensions - Json Transformation Strategy

        internal static IGraphQLQueryResults<TEntityResult> ConvertNewtonsoftJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(this JToken json, JsonSerializerSettings jsonSerializerSettings)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var newtonsoftJsonSerializer = JsonSerializer.CreateDefault(jsonSerializerSettings);

            //Dynamically parse the data from the results...
            //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
            //          & Offset Paging model is a subset of Cursor Paging (less flexible).
            GraphQLCursorPageInfo pageInfo = null;
            int? totalCount = null;
            if (json is JObject jsonObject)
            {
                pageInfo = jsonObject.Field(GraphQLFields.PageInfo)?.ToObject<GraphQLCursorPageInfo>(newtonsoftJsonSerializer);
                totalCount = (int?)jsonObject.Field(GraphQLFields.TotalCount);
            }

            //Get our Json Transformer from our Factory (which provides Caching for Types already processed)!
            var graphqlJsonTransformer = FlurlGraphQLNewtonsoftJsonTransformer.ForType<TEntityResult>();

            var transformationResults = graphqlJsonTransformer.TransformJsonForSimplifiedGraphQLModelMapping(json);

            var paginationType = transformationResults.PaginationType;
            IReadOnlyList<TEntityResult> entityResults = null;

            switch (transformationResults.Json)
            {
                case JArray arrayResults:
                    entityResults = arrayResults.ToObject<List<TEntityResult>>(newtonsoftJsonSerializer);
                    break;
                case JObject objectResult:
                    var singleEntityResult = objectResult.ToObject<TEntityResult>(newtonsoftJsonSerializer);
                    entityResults = new[] { singleEntityResult };
                    break;
            }

            switch (paginationType)
            {
                //If the results have Paging Info we map to the correct type (Connection/Cursor or CollectionSegment/Offset)...
                case PaginationType.Cursor:
                    return new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo);
                case PaginationType.Offset:
                    return new GraphQLCollectionSegmentResults<TEntityResult>(entityResults, totalCount, GraphQLOffsetPageInfo.FromCursorPageInfo(pageInfo));
                default:
                    {
                        //If we have a Total Count then we also must return a valid Paging result because it's possible to request TotalCount by itself without any other PageInfo or Nodes...
                        return totalCount.HasValue
                            ? new GraphQLConnectionResults<TEntityResult>(entityResults, totalCount, pageInfo)
                            : new GraphQLQueryResults<TEntityResult>(entityResults);
                    }
            }
        }

        #endregion
    }
}