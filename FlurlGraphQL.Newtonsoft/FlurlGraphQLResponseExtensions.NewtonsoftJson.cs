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
        /// <returns>Returns an JObject json object.</returns>
        public static async Task<JObject> ReceiveGraphQLRawNewtonsoftJsonResponse(this Task<IFlurlGraphQLResponse> responseTask)
        {
            //Now that we support multiple types of Json De-serialization we need to validate that there aren't unexpected
            //  conflicts and provide helpful error messages when a mismatch is detected; this is most common now on Newtonsoft.Json
            //  as it is not the default.
            var graphqlResponse = (FlurlGraphQLResponse)await responseTask.ConfigureAwait(false);

            if(!(graphqlResponse?.GraphQLJsonSerializer is IFlurlGraphQLNewtonsoftJsonSerializer))
                throw new InvalidOperationException(
                    $"The current GraphQL Json Serializer type [{graphqlResponse.GraphQLJsonSerializer.GetType().Name}] " +
                    $"is not compatible with Newtonsoft.Json Raw Json result type of [{nameof(JObject)}]. " +
                    $"The originating Flurl GraphQL Request must be correctly initialized with Newtonsof.Json serialization."
                );

            var results = await graphqlResponse.ProcessResponsePayloadInternalAsync(
                (responseProcessor, _) => responseProcessor.GetRawJsonData<JObject>()
            ).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// Processes/parses the results of the GraphQL query execution into a raw Json Result with all raw Json response Data available for processing.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>Returns an JObject json object.</returns>
        public static Task<JObject> ReceiveGraphQLRawNewtonsoftJsonResponse(this IFlurlGraphQLResponse response)
            => Task.FromResult(response).ReceiveGraphQLRawNewtonsoftJsonResponse();
    }
}
