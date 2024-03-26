using System.Text.Json;
using Flurl.Http;

namespace FlurlGraphQL.JsonProcessing
{
    public static class FlurlGraphQLSystemTextJsonExtensions
    {
        #region Configuration Extension - NewtonsoftJson Serializer Settings (ONLY Available after an IFlurlRequest is initialized)...

        /// <summary>
        /// Initialize a custom Json Serializer using System.Text.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="systemTextJsonOptions"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJson(this IFlurlRequest request, JsonSerializerOptions systemTextJsonOptions = null)
            => request.ToGraphQLRequest().UseGraphQLSystemTextJson(systemTextJsonOptions);

        /// <summary>
        /// Initialize a custom GraphQL Json Serializer using System.Text.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="systemTextJsonOptions"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJson(this IFlurlGraphQLRequest graphqlRequest, JsonSerializerOptions systemTextJsonOptions)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                if (systemTextJsonOptions == null && flurlGraphQLRequest.GraphQLJsonSerializer is IFlurlGraphQLSystemTextJsonSerializer existingSystemTextJsonSerializer)
                    flurlGraphQLRequest.GraphQLJsonSerializer = existingSystemTextJsonSerializer;
                else
                    flurlGraphQLRequest.GraphQLJsonSerializer = new FlurlGraphQLSystemTextJsonSerializer(
                        systemTextJsonOptions ?? FlurlGraphQLSystemTextJsonSerializer.CreateDefaultSerializerOptions()
                    );
            }

            return graphqlRequest;
        }

        #endregion
    }
}
