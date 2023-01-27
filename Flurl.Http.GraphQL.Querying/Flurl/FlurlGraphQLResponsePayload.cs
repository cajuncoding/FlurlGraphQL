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
    }
}
