using System;
using System.Collections.Generic;
using System.Linq;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL
{
    //NOTE: This is DYNAMICALLY Invoked and used at Runtime by the FlurlGraphQL.FlurlGraphQLJsonResponseProcessorFactory() class in the main FlurlGraphQL project library.
    internal class FlurlGraphQLNewtonsoftJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public static IFlurlGraphQLResponseProcessor FromFlurlGraphQLResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public FlurlGraphQLNewtonsoftJsonResponseProcessor(object data, List<GraphQLError> errors, IFlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
        {
            this.Data = data;
            this.Errors = errors?.AsReadOnly();
            this.JsonSerializer = (newtonsoftJsonSerializer as FlurlGraphQLNewtonsoftJsonSerializer).AssertArgIsNotNull(nameof(newtonsoftJsonSerializer));
        }

        #region Non-interface Properties
        public FlurlGraphQLNewtonsoftJsonSerializer JsonSerializer { get; }
        #endregion

        public object Data { get; }
        public IReadOnlyList<GraphQLError> Errors { get; }

        public IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) 
            where TResult : class
        {
            var queryResultJson = (JObject)this.Data;

            //BBernard
            //Extract the data results for the operation name specified, or first results as default (most common use case)...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? queryResultJson.FirstField()
                : queryResultJson.Field(queryOperationName);

            var typedResults = querySingleResultJson.ParseJsonToGraphQLResultsInternal<TResult>(JsonSerializer.JsonSerializerSettings);

            //Ensure that the Results we return are initialized 
            if (typedResults is GraphQLQueryResults<TResult> graphqlResults)
                typedResults = new GraphQLQueryResults<TResult>(graphqlResults, Errors);

            return typedResults;
        }

        public IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            var queryResultJson = (JObject)this.Data;

            var operationResults = queryResultJson.Properties()
                .Select(prop => new GraphQLQueryOperationResult(prop.Name, this))
                .ToList();

            return new GraphQLBatchQueryResults(operationResults);
        }

        private string _errorContentSerialized;

        public string GetErrorContent()
        {
            if (_errorContentSerialized == null)
                _errorContentSerialized = JsonSerializer.SerializeToJson(this.Errors);
            
            return _errorContentSerialized;
        }
    }
}
