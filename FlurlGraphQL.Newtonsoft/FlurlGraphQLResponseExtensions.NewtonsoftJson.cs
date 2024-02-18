using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL
{
    public static class FlurlGraphQLResponseExtensionsForNewtonsoftJson
    {
        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing as a JObject (using Newtonsoft.Json).
        /// </summary>
        /// <param name="responseTask"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static async Task<JObject> ReceiveGraphQLRawNewtonsoftJsonResponse(this Task<IFlurlGraphQLResponse> responseTask)
            => await responseTask.ProcessResponsePayloadInternalAsync((responseProcessor, _) => responseProcessor.GetRawJsonData<JObject>()).ConfigureAwait(false);

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an IGraphQLQueryResults set of typed results.</returns>
        public static Task<JObject> ReceiveGraphQLRawNewtonsoftJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawNewtonsoftJsonResponse();
    }
}
