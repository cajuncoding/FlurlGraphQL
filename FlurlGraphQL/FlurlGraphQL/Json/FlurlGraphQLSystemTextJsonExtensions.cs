using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlurlGraphQL.FlurlGraphQL.Json;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.SystemTextJsonExtensions;
using FlurlGraphQL.TypeCacheHelpers;

namespace FlurlGraphQL
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

        internal static IGraphQLQueryResults<TEntityResult> ConvertJsonToGraphQLResultsInternal<TEntityResult>(this JsonNode json, JsonSerializerOptions jsonSerializerOptions = null)
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
            //TODO: Decide if we want to keep this enforced... as it's not necessary now that we Re-Write the Json before de-serializing to the Model...
            sanitizedJsonSerializerOptions.PropertyNameCaseInsensitive = true;

            return ConvertJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(json, sanitizedJsonSerializerOptions);
        }

        internal static IGraphQLQueryResults<TEntityResult> ConvertJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(this JsonNode json, JsonSerializerOptions jsonSerializerOptions)
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

            //Get our Json Rewriter from our Factory (which provides Caching for Types already processed)!
            var graphqlJsonRewriter = FlurlGraphQLSystemTextJsonRewriter.ForType<TEntityResult>();

            var rewriterResults = graphqlJsonRewriter.RewriteJsonAsNeededForEasyGraphQLModelMapping(json);
            
            paginationType = rewriterResults.PaginationType;

            switch (rewriterResults.Json)
            {
                case JsonArray arrayResults:
                    entityResults = arrayResults.Deserialize<List<TEntityResult>>(jsonSerializerOptions);
                    break;
                case JsonObject objectResult:
                    var singleEntityResult = objectResult.Deserialize<TEntityResult>(jsonSerializerOptions);
                    entityResults = new List<TEntityResult>(capacity: 1) { singleEntityResult };
                    break;
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

        #endregion
    }
}
