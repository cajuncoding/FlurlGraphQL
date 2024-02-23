using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLSystemTextJsonSerializer
    {
        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Options directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonOptions = flurlSerializer.BruteForceGetFieldValue<JsonSerializerOptions>("_options");

            var graphqlJsonOptions = currentJsonOptions != null
                ? new JsonSerializerOptions(currentJsonOptions) //Clone existing Options if available!
                : new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; //Default Options (always Disable Case Sensitivity; which is enabled by default)

            return new FlurlGraphQLSystemTextJsonSerializer(graphqlJsonOptions);
        }

        #region Base Flurl ISerializer implementation...

        public string Serialize(object obj) => FlurlSystemTextJsonSerializer.Serialize(obj);
        public T Deserialize<T>(string s) => FlurlSystemTextJsonSerializer.Deserialize<T>(s);
        public T Deserialize<T>(Stream stream) => FlurlSystemTextJsonSerializer.Deserialize<T>(stream);

        #endregion
        
        protected DefaultJsonSerializer FlurlSystemTextJsonSerializer { get; }

        public JsonSerializerOptions JsonSerializerOptions { get; protected set; }

        public FlurlGraphQLSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            JsonSerializerOptions = jsonSerializerOptions.AssertArgIsNotNull(nameof(jsonSerializerOptions));
            FlurlSystemTextJsonSerializer = new DefaultJsonSerializer(jsonSerializerOptions);
        }

        public string SerializeToJson(object obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);

        /// <summary>
        /// Create the correct IFlurlGraphQLResponseProcessor based on this Json Serializer and the provided GraphQL Response.
        /// NOTE: This helps avoid/eliminate any additional Reflection hits to dynamically invoke Newtonsoft Json Serializers.
        /// </summary>
        /// <param name="graphqlResponse"></param>
        /// <returns></returns>
        public virtual async Task<IFlurlGraphQLResponseProcessor> CreateGraphQLResponseProcessorAsync(IFlurlGraphQLResponse graphqlResponse)
        {
            //NOTE: We use the core Flurl GetJsonAsync<>() method here to get our initial results so that we benefit from it's built in performance, simplicity, stream handling, etc.!
            var graphqlResult = await graphqlResponse.GetJsonAsync<SystemTextJsonGraphQLResult>().ConfigureAwait(false);

            return new FlurlGraphQLSystemTextJsonResponseProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlResponse.GraphQLJsonSerializer as FlurlGraphQLSystemTextJsonSerializer
            );
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    internal class SystemTextJsonGraphQLResult
    {
        [JsonPropertyName("data")]
        public JsonNode Data { get; set; }

        [JsonPropertyName("errors")]
        public List<GraphQLError> Errors { get; set; }
    }
}
