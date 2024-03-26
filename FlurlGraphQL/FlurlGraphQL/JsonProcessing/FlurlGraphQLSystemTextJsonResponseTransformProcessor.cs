using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLSystemTextJsonResponseTransformProcessor : IFlurlGraphQLResponseProcessor
    {
        public FlurlGraphQLSystemTextJsonResponseTransformProcessor(JsonObject rawDataJsonNode, List<GraphQLError> errors, FlurlGraphQLSystemTextJsonSerializer systemTextJsonSerializer)
        {
            this.RawDataJsonObject = rawDataJsonNode;
            this.Errors = errors?.AsReadOnly();
            this.GraphQLJsonSerializer = systemTextJsonSerializer.AssertArgIsNotNull(nameof(systemTextJsonSerializer));
        }

        #region Non-interface Properties
        public FlurlGraphQLSystemTextJsonSerializer GraphQLJsonSerializer { get; }
        #endregion

        protected JsonObject RawDataJsonObject { get; }
        protected IReadOnlyList<GraphQLError> Errors { get; }
        protected string ErrorContentSerialized { get; set; }


        public TJson GetRawJsonData<TJson>()
            => this.RawDataJsonObject is TJson rawDataJson
                ? rawDataJson
                : throw new ArgumentOutOfRangeException(
                    nameof(TJson), 
                    $"Invalid type [{typeof(TJson).Name}] was specified; expected type <{nameof(JsonNode)}> as the supported type for Raw Json when using System.Text.Json Serialization."
                );

        public virtual IReadOnlyList<GraphQLError> GetGraphQLErrors() => this.Errors;

        public virtual IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class
        {
            var rawDataJson = (JsonNode)this.RawDataJsonObject;

            //BBernard
            //Extract the data results for the operation name specified, or first results as default (most common use case)...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? rawDataJson.AsObject().FirstOrDefault().Value
                : rawDataJson[queryOperationName];

            var typedResults = ConvertSystemTextJsonToGraphQLResultsWithJsonSerializerInternal<TResult>(querySingleResultJson, GraphQLJsonSerializer.JsonSerializerOptions);

            //Ensure that the Results we return are initialized along with any potential Errors (that have already been parsed/captured)... 
            if (this.Errors != null && typedResults is GraphQLQueryResults<TResult> graphqlResults)
                graphqlResults.Errors = this.Errors;

            return typedResults;
        }

        public virtual IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            var operationResults = this.RawDataJsonObject
                .Select(prop => new GraphQLQueryOperationResult(prop.Key, this))
                .ToList();

            return new GraphQLBatchQueryResults(operationResults);
        }

        public virtual string GetErrorContent()
            => ErrorContentSerialized ?? (ErrorContentSerialized = GraphQLJsonSerializer.Serialize(this.Errors));

        protected virtual IGraphQLQueryResults<TEntityResult> ConvertSystemTextJsonToGraphQLResultsWithJsonSerializerInternal<TEntityResult>(JsonNode json, JsonSerializerOptions jsonSerializerOptions) 
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

            //Get our Json Transformer from our Factory (which provides Caching for Types already processed)!
            var graphqlJsonTransformer = FlurlGraphQLSystemTextJsonTransformer.ForType<TEntityResult>();

            var transformResults = graphqlJsonTransformer.TransformJsonForSimplifiedGraphQLModelMapping(json);

            var paginationType = transformResults.PaginationType;
            IReadOnlyList<TEntityResult> entityResults = null;

            switch (transformResults.Json)
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

    }
}
