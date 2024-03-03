using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlurlGraphQL
{
    public static class FlurlGraphQLResponseExtensionsForSystemTextJson
    {
        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing as a JsonNode (using System.Text.Json).
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<JsonObject> ReceiveGraphQLRawSystemTextJsonResponse(this Task<IFlurlGraphQLResponse> responseTask)
        {
            //Now that we support multiple types of Json De-serialization we need to validate that there aren't unexpected
            //  conflicts and provide helpful error messages when a mismatch is detected; this is most common now on Newtonsoft.Json
            //  as it is not the default.
            var graphqlResponse = (FlurlGraphQLResponse)await responseTask.ConfigureAwait(false);

            if (!(graphqlResponse?.GraphQLJsonSerializer is IFlurlGraphQLSystemTextJsonSerializer))
                throw new InvalidOperationException(
                    $"The current GraphQL Json Serializer type [{graphqlResponse.GraphQLJsonSerializer.GetType().Name}] " +
                    $"is not compatible with System.Text.Json Raw Json result type of [{nameof(JsonObject)}]. " +
                    $"The originating Flurl GraphQL Request must be correctly initialized with System.Text.Json serialization."
                );

            var results = await graphqlResponse.ProcessResponsePayloadInternalAsync(
                (responseProcessor, _) => responseProcessor.GetRawJsonData<JsonObject>()
            ).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<JsonObject> ReceiveGraphQLRawSystemTextJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawSystemTextJsonResponse();
    }
}
