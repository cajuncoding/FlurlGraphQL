﻿using System;
using System.Collections.Generic;
using System.Linq;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL
{
    public class FlurlGraphQLNewtonsoftJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public static IFlurlGraphQLResponseProcessor FromFlurlGraphQLResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public FlurlGraphQLNewtonsoftJsonResponseProcessor(object data, List<GraphQLError> errors, IReadOnlyDictionary<string, object> contextBag, IFlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
        {
            this.Data = data;
            this.Errors = errors?.AsReadOnly();
            //We MUST to pass along the ContextBag (internal) which may contain configuration details for processing the payload results...
            this.ContextBag = contextBag;
            this.JsonSerializer = newtonsoftJsonSerializer.AssertArgIsNotNull(nameof(newtonsoftJsonSerializer));
        }

        #region Non-interface Properties
        public IFlurlGraphQLJsonSerializer JsonSerializer { get; }
        #endregion

        public object Data { get; }
        public IReadOnlyList<GraphQLError> Errors { get; }
        public IReadOnlyDictionary<string, object> ContextBag { get; set; }

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

            var jsonSerializerSettings = ContextBag?.TryGetValue(ContextItemKeys.NewtonsoftJsonSerializerSettings, out var serializerSettings) ?? false
                ? serializerSettings as JsonSerializerSettings
                : null;

            var typedResults = querySingleResultJson.ParseJsonToGraphQLResultsInternal<TResult>(jsonSerializerSettings);

            if (typedResults is GraphQLQueryResults<TResult> graphqlResults)
            {
                #pragma warning disable CS0618
                graphqlResults.SetErrorsInternal(Errors);
                #pragma warning restore CS0618
            }

            return typedResults;
        }

        public IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public IGraphQLBatchQueryResults LoadGraphQLBatchQueryResults()
        {
            //BBernard
            //Extract the Collection Data specified... or first data...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
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