using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlurlGraphQL
{
    public static class FlurlGraphQLResponseExtensionsForSystemTextJson
    {
        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing as a JsonNode (using System.Text.Json).
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<JsonNode> ReceiveGraphQLRawSystemTextJsonResponse(this Task<IFlurlGraphQLResponse> responseTask)
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _) => responseProcessor.Data as JsonNode).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<JsonNode> ReceiveGraphQLRawSystemTextJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawSystemTextJsonResponse();
    }
}
