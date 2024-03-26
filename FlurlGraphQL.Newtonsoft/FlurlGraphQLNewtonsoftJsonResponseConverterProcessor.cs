using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.TypeCacheHelpers;

namespace FlurlGraphQL.JsonProcessing
{
    [Obsolete("This is the original/legacy approach to processing Newtonsoft Json via custom converter but is now replaced by the new FlurlGraphQLNewtonsoftJsonResponseTransformProcessor " +
                        "which is optimized and benchmarked to be ~2X faster at processing Json with Newtonsoft.Json")]
    internal class FlurlGraphQLNewtonsoftJsonResponseConverterProcessor : FlurlGraphQLNewtonsoftJsonResponseBaseProcessor, IFlurlGraphQLResponseProcessor
    {
        internal FlurlGraphQLNewtonsoftJsonResponseConverterProcessor(JObject rawDataJObject, List<GraphQLError> errors, FlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
            : base(rawDataJObject, errors, newtonsoftJsonSerializer)
        {
        }

        public override IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null)
        {
            var rawDataJson = this.RawDataJObject;

            //BBernard
            //Extract the data results for the operation name specified, or first results as default (most common use case)...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? rawDataJson.FirstField()
                : rawDataJson.Field(queryOperationName);

            var typedResults = ParseJsonToGraphQLResultsInternal<TResult>(querySingleResultJson, GraphQLJsonSerializer.JsonSerializerSettings);

            //Ensure that the Results we return are initialized along with any potential Errors (that have already been parsed/captured)... 
            if (this.Errors != null && typedResults is GraphQLQueryResults<TResult> graphqlResults)
                graphqlResults.Errors = this.Errors;

            return typedResults;
        }

        internal IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsInternal<TEntityResult>(JToken json, JsonSerializerSettings jsonSerializerSettings = null)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Ensure that all json parsing uses a Serializer with the GraphQL Contract Resolver...
            //NOTE: We still support normal Serializer Default settings via Newtonsoft framework!
            var jsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault(jsonSerializerSettings);
            jsonSerializer.Converters.Add(new FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter());

            return ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(json, jsonSerializer);
        }

        internal IGraphQLQueryResults<TEntityResult> ParseJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(JToken json, JsonSerializer jsonSerializer)
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
    }
}
