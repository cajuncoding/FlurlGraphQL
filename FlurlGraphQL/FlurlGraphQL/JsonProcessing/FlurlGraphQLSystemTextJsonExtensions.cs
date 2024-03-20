using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlurlGraphQL.SystemTextJsonExtensions;

namespace FlurlGraphQL.JsonProcessing
{
    public static class FlurlGraphQLSystemTextJsonExtensions
    {
        public static bool IsNullOrUndefined(this JsonNode jsonNode)
        {
            var jsonValueKind = jsonNode?.GetValueKind() ?? JsonValueKind.Null;
            return jsonValueKind == JsonValueKind.Null || jsonValueKind == JsonValueKind.Undefined;
        }

        public static bool IsNotNullOrUndefined(this JsonNode jsonNode) => !jsonNode.IsNullOrUndefined();

        #region Json Parsing Extensions

        internal static IGraphQLQueryResults<TEntityResult> ConvertSystemTextJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(this JsonNode json, JsonSerializerOptions jsonSerializerOptions)
            where TEntityResult : class
        {
            if (json == null)
                return new GraphQLQueryResults<TEntityResult>();

            //Dynamically parse the data from the results...
            //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
            //          & Offset Paging model is a subset of Cursor Paging (less flexible).
            GraphQLCursorPageInfo pageInfo = null;
            int? totalCount = null;
            if (json is JsonObject jsonObject)
            {
                pageInfo = jsonObject[GraphQLFields.PageInfo]?.Deserialize<GraphQLCursorPageInfo>(jsonSerializerOptions);
                totalCount = (int?)jsonObject[GraphQLFields.TotalCount];
            }

            //Get our Json Rewriter from our Factory (which provides Caching for Types already processed)!
            var graphqlJsonRewriter = FlurlGraphQLSystemTextJsonRewriter.ForType<TEntityResult>();

            var rewriterResults = graphqlJsonRewriter.RewriteJsonForSimplifiedGraphQLModelMapping(json);
            
            var paginationType = rewriterResults.PaginationType;
            IReadOnlyList<TEntityResult> entityResults = null;

            switch (rewriterResults.Json)
            {
                case JsonArray arrayResults:
                    entityResults = arrayResults.Deserialize<TEntityResult[]>(jsonSerializerOptions);
                    break;
                case JsonObject objectResult:
                    var singleEntityResult = objectResult.Deserialize<TEntityResult>(jsonSerializerOptions);
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
