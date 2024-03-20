using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLSystemTextJsonSerializer
    {
        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Options directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonOptions = flurlSerializer.BruteForceGetFieldValue<JsonSerializerOptions>("_options");

            //Clone existing Options if available so we isolate our custom changes needed for GraphQL
            //  (e.g. custom String Enum converter, Case-insensitive, etc.)!
            var graphqlJsonOptions = currentJsonOptions != null
                ? new JsonSerializerOptions(currentJsonOptions)
                : CreateDefaultSerializerOptions();

            //For compatibility with FlurlGraphQL v1 behavior (using Newtonsoft.Json) we always enable case-insensitive Field Matching with System.Text.Json.
            //This is also helpful since GraphQL Json (and Json in general) use CamelCase and nearly always mismatch C# Naming Pascal Case standards of C# Class Models, etc...
            //NOTE: WE are operating on a copy of the original Json Settings so this does NOT mutate the core/original settings from Flurl or those specified for the GraphQL request, etc.
            graphqlJsonOptions.PropertyNameCaseInsensitive = true;

            //For compatibility with FlurlGraphQL v1 behavior (using Newtonsoft.Json) we need to provide support for String to Enum conversion along with support for enum annotations
            //  via [EnumMember(Value ="CustomName")] annotation (compatible with Newtonsoft.Json). In addition we now also support [Description("CustomName")] annotation for
            //  easier syntax that is arguably more intuitive to use.
            graphqlJsonOptions.Converters.Add(new JsonStringEnumMemberConverter(allowIntegerValues: true));

            return new FlurlGraphQLSystemTextJsonSerializer(graphqlJsonOptions);
        }

        public static JsonSerializerOptions CreateDefaultSerializerOptions() 
            => new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

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
            var systemTextJsonGraphQLResult = await graphqlResponse.GetJsonAsync<SystemTextJsonGraphQLResult>().ConfigureAwait(false);
            return CreateGraphQLResponseProcessor(systemTextJsonGraphQLResult);
        }

        /// <summary>
        /// Internal Helper to Create the correct IFlurlGraphQLResponseProcessor based on this Json Serializer and the provided System.Text.Json GraphQL Result.
        /// </summary>
        /// <param name="systemTextJsonGraphQLResult"></param>
        /// <returns></returns>
        internal virtual IFlurlGraphQLResponseProcessor CreateGraphQLResponseProcessor(SystemTextJsonGraphQLResult systemTextJsonGraphQLResult)
            => new FlurlGraphQLSystemTextJsonResponseRewriteProcessor(systemTextJsonGraphQLResult.Data, systemTextJsonGraphQLResult.Errors, this);

        /// <summary>
        /// Parses only the Errors from a GraphQL response. Used when Flurl throws and HttpException that still contains a valid 
        /// GraphQL Json response.
        /// </summary>
        /// <param name="errorContent"></param>
        /// <returns></returns>
        public virtual IReadOnlyList<GraphQLError> ParseErrorsFromGraphQLExceptionErrorContent(string errorContent)
        {
            var graphqlResult = Deserialize<SystemTextJsonGraphQLResult>(errorContent);
            return graphqlResult.Errors;
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    internal class SystemTextJsonGraphQLResult
    {
        [JsonPropertyName("data")]
        public JsonObject Data { get; set; }

        [JsonPropertyName("errors")]
        public List<GraphQLError> Errors { get; set; }
    }
}
