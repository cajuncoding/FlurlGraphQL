using System;
using System.Text.Json;
using Flurl.Http;
using FlurlGraphQL.JsonProcessing;

//NOTE: To ensure these Extensions are readily discoverable we use the root FlurlGraphQL namespace!
namespace FlurlGraphQL
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
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJson(this IFlurlGraphQLRequest graphqlRequest, JsonSerializerOptions systemTextJsonOptions = null)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                flurlGraphQLRequest.GraphQLJsonSerializer = new FlurlGraphQLSystemTextJsonSerializer(
                    systemTextJsonOptions ?? FlurlGraphQLSystemTextJsonSerializer.CreateDefaultSerializerOptions()
                );
            }

            return graphqlRequest;
        }

        /// <summary>
        /// Initialize a custom Json Serializer using System.Text.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="configureJsonSerializerOptions">Action method to configure the existing options as needed.</param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJson(this IFlurlRequest request, Action<JsonSerializerOptions> configureJsonSerializerOptions)
            => request.ToGraphQLRequest().UseGraphQLSystemTextJson(configureJsonSerializerOptions);

        /// <summary>
        /// Configure the GraphQL Json Serializer options using System.Text.Json, but only for this GraphQL request; isolated from any other GraphQL Requests.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="configureJsonSerializerOptions">Action method to configure the existing options as needed.</param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJson(this IFlurlGraphQLRequest graphqlRequest, Action<JsonSerializerOptions> configureJsonSerializerOptions)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
            {
                var graphqlJsonSerializerOptions = FlurlGraphQLSystemTextJsonSerializer.CreateDefaultSerializerOptions();
                configureJsonSerializerOptions?.Invoke(graphqlJsonSerializerOptions);
                flurlGraphQLRequest.UseGraphQLSystemTextJson(graphqlJsonSerializerOptions);
            }

            return graphqlRequest;
        }

        #endregion
    }
}
