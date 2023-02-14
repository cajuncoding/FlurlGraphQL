using Newtonsoft.Json;

namespace FlurlGraphQL.Querying
{
    internal class FlurlGraphQLRequestPayload
    {
        public FlurlGraphQLRequestPayload(string query, object variables)
        {
            this.Query = query;
            this.Variables = variables;
        }

        //NOTE: To eliminate dependencies on Json.Net attributes, etc. this payload intentionally
        //      uses the required lowercase names for valid GraphQL request.
        [JsonProperty("query")]
        public string Query { get; }
        
        [JsonProperty("variables")]
        public object Variables { get; }
    }
}
