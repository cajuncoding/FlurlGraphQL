using System;
using System.Collections.Generic;
using System.Linq;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.JsonProcessing
{
    public abstract class FlurlGraphQLNewtonsoftJsonResponseBaseProcessor : IFlurlGraphQLResponseProcessor
    {
        protected FlurlGraphQLNewtonsoftJsonResponseBaseProcessor(JObject rawDataJObject, List<GraphQLError> errors, FlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
        {
            this.RawDataJObject = rawDataJObject;
            this.Errors = errors?.AsReadOnly();
            this.JsonSerializer = newtonsoftJsonSerializer.AssertArgIsNotNull(nameof(newtonsoftJsonSerializer));
        }

        #region Non-interface Properties
        public FlurlGraphQLNewtonsoftJsonSerializer JsonSerializer { get; }
        #endregion

        protected JObject RawDataJObject { get; }
        protected IReadOnlyList<GraphQLError> Errors { get; }
        protected string ErrorContentSerialized { get; set; }

        public TJson GetRawJsonData<TJson>() => this.RawDataJObject is TJson rawDataJson
            ? rawDataJson
            : throw new ArgumentOutOfRangeException(
                nameof(TJson),
                $"Invalid type [{typeof(TJson).Name}] was specified; expected type <{nameof(JObject)}> as the supported type for Raw Json when using Newtonsoft.Json Serialization."
            );

        public virtual IReadOnlyList<GraphQLError> GetGraphQLErrors() => this.Errors;

        public abstract IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class;

        public virtual IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            var operationResults = this.RawDataJObject.Properties()
                .Select(prop => new GraphQLQueryOperationResult(prop.Name, this))
                .ToList();

            return new GraphQLBatchQueryResults(operationResults);
        }

        public virtual string GetErrorContent()
            => ErrorContentSerialized ?? (ErrorContentSerialized = JsonSerializer.Serialize(this.Errors));
    }
}
