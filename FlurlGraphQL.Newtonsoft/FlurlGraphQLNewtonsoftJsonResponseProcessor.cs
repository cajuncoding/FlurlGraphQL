using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL
{
    public class FlurlGraphQLNewtonsoftJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public FlurlGraphQLNewtonsoftJsonResponseProcessor(JObject rawDataJObject, List<GraphQLError> errors, FlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
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

        public TJson GetRawJsonData<TJson>() => this.RawDataJObject is TJson rawDataJson
            ? rawDataJson
            : throw new ArgumentOutOfRangeException(
                nameof(TJson),
                $"Invalid type [{typeof(TJson).Name}] was specified; expected type <{nameof(JObject)}> as the supported type for Raw Json using Newtonsoft.Json Serialization."
            );

        public virtual IReadOnlyList<GraphQLError> GetGraphQLErrors() => this.Errors;

        public virtual IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class
        {
            var rawDataJson = (JObject)this.RawDataJObject;

            //BBernard
            //Extract the data results for the operation name specified, or first results as default (most common use case)...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? rawDataJson.FirstField()
                : rawDataJson.Field(queryOperationName);

            var typedResults = querySingleResultJson.ParseJsonToGraphQLResultsInternal<TResult>(JsonSerializer.JsonSerializerSettings);

            //Ensure that the Results we return are initialized along with any potential Errors (that have already been parsed/captured)... 
            if (this.Errors != null && typedResults is GraphQLQueryResults<TResult> graphqlResults)
                graphqlResults.Errors = this.Errors;

            return typedResults;
        }

        public virtual IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            var operationResults = this.RawDataJObject.Properties()
                .Select(prop => new GraphQLQueryOperationResult(prop.Name, this))
                .ToList();

            return new GraphQLBatchQueryResults(operationResults);
        }

        protected string _errorContentSerialized;

        public virtual string GetErrorContent()
        {
            if (_errorContentSerialized == null)
                _errorContentSerialized = JsonSerializer.Serialize(this.Errors);
            
            return _errorContentSerialized;
        }
    }
}
