using System;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying
{
    internal class FlurlGraphQLRequestPayload
    {
        public FlurlGraphQLRequestPayload(GraphQLQueryType graphqlQueryType, string queryOrId, object variables)
        {
            switch(graphqlQueryType)
            {
                case GraphQLQueryType.PersistedQuery: this.Id = queryOrId; break;
                case GraphQLQueryType.Query: this.Query = queryOrId; break;
                default: throw new ArgumentOutOfRangeException(nameof(graphqlQueryType), $"GraphQL Query Type [{graphqlQueryType}] cannot be initialized.");
            }; 
            
            this.Variables = variables;
        }

        //NOTE: To eliminate dependencies on Json.Net attributes, etc. this payload intentionally
        //      uses the required lowercase names for valid GraphQL request.
        [JsonProperty("query")]
        public string Query { get; }

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("variables")]
        public object Variables { get; }
    }
}
