using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public FlurlGraphQLSystemTextJsonResponseProcessor(JsonObject rawDataJsonNode, List<GraphQLError> errors, FlurlGraphQLSystemTextJsonSerializer systemTextJsonSerializer)
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

            var typedResults = querySingleResultJson.ConvertSystemTextJsonToGraphQLResultsWithJsonSerializerInternal<TResult>(GraphQLJsonSerializer.JsonSerializerOptions);

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

    }
}
