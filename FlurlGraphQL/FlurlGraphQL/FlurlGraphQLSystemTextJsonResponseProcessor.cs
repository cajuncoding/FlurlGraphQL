using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public static IFlurlGraphQLResponseProcessor FromFlurlGraphQLResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public FlurlGraphQLSystemTextJsonResponseProcessor(JsonNode rawDataJsonNode, List<GraphQLError> errors, IFlurlGraphQLSystemTextJsonSerializer systemTextJsonSerializer)
        {
            this.RawDataJsonNode = rawDataJsonNode;
            this.Errors = errors?.AsReadOnly();
            this.JsonSerializer = systemTextJsonSerializer.AssertArgIsNotNull(nameof(systemTextJsonSerializer));
        }

        #region Non-interface Properties
        public IFlurlGraphQLJsonSerializer JsonSerializer { get; }
        #endregion

        protected JsonNode RawDataJsonNode { get; }
        protected IReadOnlyList<GraphQLError> Errors { get; }

        public TJson GetRawJsonData<TJson>()
            => this.RawDataJsonNode is TJson rawDataJson
                ? rawDataJson
                : throw new ArgumentOutOfRangeException(
                    nameof(TJson), 
                    $"Invalid type [{typeof(TJson).Name}] was specified; expected type <{nameof(JsonNode)}> as the supported type for Raw Json using System.Text.Json Serialization."
                );

        public virtual IReadOnlyList<GraphQLError> GetGraphQLErrors() => this.Errors;

        public virtual IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public virtual IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public virtual string GetErrorContent()
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }
    }
}
