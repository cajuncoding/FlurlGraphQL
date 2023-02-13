using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Querying
{
    internal class FlurlGraphQLResponsePayload
    {
        public FlurlGraphQLResponsePayload(JObject data, List<GraphQLError> errors)
        {
            this.Data = data;
            this.Errors = errors?.AsReadOnly();
        }

        //NOTE: To eliminate dependencies on Json.Net attributes, etc. this payload intentionally
        //      uses the required lowercase names for valid GraphQL request.
        [JsonProperty("data")]
        public JObject Data { get; }
        
        [JsonProperty("errors")]
        public IReadOnlyList<GraphQLError> Errors { get; }

        public Dictionary<string, object> ContextBag { get; set; }

        public IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) 
            where TResult : class
        {
            //BBernard
            //Extract the Collection Data specified... or first data...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var queryResultJson = Data;

            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? queryResultJson.FirstField()
                : queryResultJson.Field(queryOperationName);

            var jsonSerializerSettings = ContextBag?.TryGetValue(nameof(JsonSerializerSettings), out var serializerSettings) ?? false
                ? serializerSettings as JsonSerializerSettings 
                : null;

            var typedResults = querySingleResultJson.ParseJsonToGraphQLResultsInternal<TResult>(jsonSerializerSettings);
            return typedResults;
        }
    }
}
