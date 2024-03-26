using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLNewtonsoftJsonSerializer : IFlurlGraphQLNewtonsoftJsonSerializer
    {
        //NOTE: This is DYNAMICALLY Invoked and used at Runtime by the FlurlGraphQLJsonSerializerFactory class.
        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Settings directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonSettings = flurlSerializer.BruteForceGetFieldValue<JsonSerializerSettings>("_settings");
            
            var graphqlJsonSettings = currentJsonSettings != null
                //Clone existing Options if available!
                ? new JsonSerializerSettings(currentJsonSettings)
                //Default Options are used as fallback...
                : CreateDefaultSerializerSettings();

            return new FlurlGraphQLNewtonsoftJsonSerializer(graphqlJsonSettings);
        }

        public static JsonSerializerSettings CreateDefaultSerializerSettings()
            => JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

        #region Base Flurl ISerializer implementation...

        public string Serialize(object obj) => FlurlNewtonsoftSerializer.Serialize(obj);
        public T Deserialize<T>(string s) => FlurlNewtonsoftSerializer.Deserialize<T>(s);
        public T Deserialize<T>(Stream stream) => FlurlNewtonsoftSerializer.Deserialize<T>(stream);

        #endregion

        protected NewtonsoftJsonSerializer FlurlNewtonsoftSerializer { get; }

        public JsonSerializerSettings JsonSerializerSettings { get; protected set; }

        public FlurlGraphQLNewtonsoftJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
        {
            JsonSerializerSettings = jsonSerializerSettings.AssertArgIsNotNull(nameof(jsonSerializerSettings));
            FlurlNewtonsoftSerializer = new NewtonsoftJsonSerializer(jsonSerializerSettings);
        }

        /// <summary>
        /// Create the correct IFlurlGraphQLResponseProcessor based on this Json Serializer and the provided GraphQL Response.
        /// NOTE: This helps avoid/eliminate any additional Reflection hits to dynamically invoke Newtonsoft Json Serializers.
        /// </summary>
        /// <param name="graphqlResponse"></param>
        /// <returns></returns>
        public virtual async Task<IFlurlGraphQLResponseProcessor> CreateGraphQLResponseProcessorAsync(IFlurlGraphQLResponse graphqlResponse)
        {
            //NOTE: We use the core Flurl GetJsonAsync<>() method here to get our initial results so that we benefit from it's built in performance, simplicity, stream handling, etc.!
            var newtonsoftGraphQLResult = await graphqlResponse.GetJsonAsync<NewtonsoftGraphQLResult>().ConfigureAwait(false);
            return CreateGraphQLResponseProcessor(newtonsoftGraphQLResult);
        }

        /// <summary>
        /// Internal Helper to Create the correct IFlurlGraphQLResponseProcessor based on this Json Serializer and the provided Newtonsoft GraphQL Result.
        /// </summary>
        /// <param name="newtonsoftGraphQLResult"></param>
        /// <returns></returns>
        internal IFlurlGraphQLResponseProcessor CreateGraphQLResponseProcessor(NewtonsoftGraphQLResult newtonsoftGraphQLResult)
            => new FlurlGraphQLNewtonsoftJsonResponseTransformProcessor(newtonsoftGraphQLResult.Data, newtonsoftGraphQLResult.Errors, this);

        /// <summary>
        /// Parses only the Errors from a GraphQL response. Used when Flurl throws and HttpException that still contains a valid 
        /// GraphQL Json response.
        /// </summary>
        /// <param name="errorContent"></param>
        /// <returns></returns>
        public virtual IReadOnlyList<GraphQLError> ParseErrorsFromGraphQLExceptionErrorContent(string errorContent)
        {
            var graphqlResult = Deserialize<NewtonsoftGraphQLResult>(errorContent);
            return graphqlResult.Errors;
        }
    }

    internal class NewtonsoftGraphQLResult
    {
        [JsonProperty("data")]
        public JObject Data { get; set; }

        [JsonProperty("errors")]
        public List<GraphQLError> Errors { get; set; }
    }
}
