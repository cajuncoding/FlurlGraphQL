using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.SystemTextJsonExtensions;
using FlurlGraphQL.TypeCacheHelpers;

namespace FlurlGraphQL
{
    public static class FlurlGraphQLSystemTextJsonExtensions
    {
        #region Json Parsing Extensions

        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsInternal<TEntityResult>(this JsonNode json, JsonSerializerOptions jsonSerializerOptions = null)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var sanitizedJsonSerializerOptions = jsonSerializerOptions == null 
                ? new JsonSerializerOptions() 
                : new JsonSerializerOptions(jsonSerializerOptions);

            //Must ALWAYS enable Case-insensitive Field Matching since GraphQL Json (and Json in general) use CamelCase and nearly always mismatch C# Naming Pascal Case standards...
            //NOTE: WE are operating on a copy of the original Json Settings so this does NOT mutate the core/original settings from Flurl or those specified for the GraphQL request, etc.
            sanitizedJsonSerializerOptions.PropertyNameCaseInsensitive = true;
            sanitizedJsonSerializerOptions.Converters.Add(new FlurlGraphQLSystemTextJsonPaginatedResultsConverterFactory());

            return ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(json, sanitizedJsonSerializerOptions);
        }

        internal static IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(this JsonNode json, JsonSerializerOptions jsonSerializerOptions)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Dynamically parse the data from the results...
            //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
            //          & Offset Paging model is a subset of Cursor Paging (less flexible).
            var pageInfo = json[GraphQLFields.PageInfo]?.Deserialize<GraphQLCursorPageInfo>(jsonSerializerOptions);
            var totalCount = (int?)json[GraphQLFields.TotalCount];

            PaginationType? paginationType = null;
            List<TEntityResult> entityResults = null;

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            if (json[GraphQLFields.Nodes] is JsonArray nodesJson)
            {
                entityResults = nodesJson.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                paginationType = PaginationType.Cursor;
            }
            else if (json[GraphQLFields.Items] is JsonArray itemsJson)
            {
                entityResults = itemsJson.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                paginationType = PaginationType.Offset;
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json[GraphQLFields.Edges] is JsonArray edgesJson)
            {
                paginationType = PaginationType.Cursor;
                var entityType = typeof(TEntityResult);

                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
                if (entityType.IsDerivedFromGenericParent(GraphQLTypeCache.CachedIGraphQLEdgeGenericType))
                {
                    //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
                    //entityResults = edges.Select(edge => edge?.Deserialize<TEntityResult>(jsonSerializerOptions)).ToList();
                    entityResults = edgesJson.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                }
                //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
                else
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                }
            }
            else
            {
                switch (json)
                {
                    case JsonArray arrayResults:
                        entityResults = arrayResults.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                        break;
                    //TODO: Test out FirstOrDefault()???
                    case JsonObject jsonObj when jsonObj.Count > 0 && jsonObj[0] is JsonArray firstArrayResults:
                        entityResults = firstArrayResults.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                        break;
                    //If only a single Object was returned then this is likely a Mutation so we return the single
                    //  item as the first-and-only result of the set...
                    case JsonObject jsonObj:
                        var singleResult = jsonObj.Deserialize<TEntityResult>(jsonSerializerOptions);
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

        internal static JsonArray FlattenGraphQLEdgesJsonToArrayOfNodes(this JsonArray edgesJson)
        {
            var edgeNodesArray = edgesJson
                .OfType<JsonObject>()
                .Select(edge =>
                {
                    //NOW we must MOVE / Re-locate all Nodes into our output JsonArray which means we have to remove them from the Parent
                    //  to avoid "Node already has a Parent" exceptions...
                    var node = edge[GraphQLFields.Node];
                    edge.Remove(GraphQLFields.Node);

                    //If not already defined, we map the Edges Cursor value to the Node so that the model is simplified
                    //  and any consumer can just add a "Cursor" property to their model to get the node's cursor.
                    if (node != null && node[GraphQLFields.Cursor] == null && edge[GraphQLFields.Cursor] is JsonValue cursorJsonValue)
                        node.AsObject().Add(GraphQLFields.Cursor, cursorJsonValue.ToString());

                    return node;
                })
                .Where(n => n != null && !n.IsNullOrUndefinedJson())
                .ToArray();

            var edgeNodesJson = new JsonArray(edgeNodesArray.ToArray());
            return edgeNodesJson;
        }

        public static bool IsNullOrUndefinedJson(this JsonNode jsonNode)
        {
            var jsonValueKind = jsonNode.GetValueKind();
            return jsonValueKind == JsonValueKind.Null || jsonValueKind == JsonValueKind.Undefined;
        }

        #endregion
    }
}
