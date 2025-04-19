using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.JsonProcessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//NOTE: To ensure these Extensions are readily discoverable we use the root FlurlGraphQL namespace!
namespace FlurlGraphQL
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
        public static IFlurlGraphQLRequest UseGraphQLNewtonsoftJson(this IFlurlGraphQLRequest graphqlRequest, JsonSerializerSettings newtonsoftJsonSettings = null)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                flurlGraphQLRequest.GraphQLJsonSerializer = new FlurlGraphQLNewtonsoftJsonSerializer(
                    newtonsoftJsonSettings ?? FlurlGraphQLNewtonsoftJsonSerializer.CreateDefaultSerializerSettings()
                );
            }

            return graphqlRequest;
        }

        /// <summary>
        /// Initialize a custom GraphQL Json Serializer using Newtonsoft.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="configureJsonSerializerSettings">Action method to configure the existing options as needed.</param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLNewtonsoftJson(this IFlurlRequest request, Action<JsonSerializerSettings> configureJsonSerializerSettings)
            => request.ToGraphQLRequest().UseGraphQLNewtonsoftJson(configureJsonSerializerSettings);

        /// <summary>
        /// Configure the GraphQL Json Serializer settings using Newtonsoft.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="configureJsonSerializerSettings">Action method to configure the existing options as needed.</param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLNewtonsoftJson(this IFlurlGraphQLRequest graphqlRequest, Action<JsonSerializerSettings> configureJsonSerializerSettings)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                var graphqlJsonSerializerOptions = FlurlGraphQLNewtonsoftJsonSerializer.CreateDefaultSerializerSettings();
                configureJsonSerializerSettings?.Invoke(graphqlJsonSerializerOptions);
                flurlGraphQLRequest.UseGraphQLNewtonsoftJson(graphqlJsonSerializerOptions);
            }

            return graphqlRequest;
        }

        #endregion

        #region Json Parsing Extensions - JsonConvert Strategy

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