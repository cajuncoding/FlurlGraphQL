using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            //Create the GraphQL Json Serializer options with critical default configuration being enforced!
            var graphqlJsonOptions = CreateDefaultSerializerOptions(currentJsonOptions);

            return new FlurlGraphQLSystemTextJsonSerializer(graphqlJsonOptions);
        }

        public static JsonSerializerOptions CreateDefaultSerializerOptions(JsonSerializerOptions originalJsonOptions = null)
        {
            //Clone existing Options if available so we isolate our custom changes needed for GraphQL (e.g. custom String Enum converter, Case-insensitive, etc.)!
            var graphqlJsonOptions = originalJsonOptions != null
                ? new JsonSerializerOptions(originalJsonOptions)
                : new JsonSerializerOptions();


            var defaultJsonConfig = FlurlGraphQLConfig.DefaultConfig;

            //For compatibility with FlurlGraphQL v1 behavior (using Newtonsoft.Json) we always enable case-insensitive Field Matching with System.Text.Json.
            //This is also helpful since GraphQL Json (and Json in general) use CamelCase and nearly always mismatch C# Naming Pascal Case standards of C# Class Models, etc...
            //NOTE: WE are operating on a copy of the original Json Settings so this does NOT mutate the core/original settings from Flurl or those specified for the GraphQL request, etc.
            if(defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableCaseInsensitiveJsonHandling))
                graphqlJsonOptions.PropertyNameCaseInsensitive = true;

            //For compatibility when actually serializing we still need to enforce CamelCase (as noted above for parsing/de-serializing) because most JSON frameworks
            //  (e.g. HotChocolate for .NET) the GraphQL json is generally expected to be in camelCase format otherwise parsing on the GraphQL Server side may fail.
            //This is likely a critical element missed by many developers so we enable it by default here to streamline and simplify working with GraphQL.
            if (defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableCamelCaseSerialization))
                graphqlJsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            //For compatibility with FlurlGraphQL v1 behavior (using Newtonsoft.Json) we need to provide support for String to Enum conversion along with support for enum annotations
            //  via [EnumMember(Value ="CustomName")] annotation (compatible with Newtonsoft.Json). In addition, we now also inherently support [JsonPropertyName("CustomName")] annotation for
            //  easier syntax that is arguably more intuitive to use as this is provided natively by System.Text.Json.
            if (defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableStringEnumHandling))
            {
                //NOTE: For performance we KNOW we need to add this if the original options were not provided (e.g. null)...
                if (originalJsonOptions is null || !originalJsonOptions.Converters.OfType<JsonStringEnumMemberConverter>().Any())
                {
                    //To simplify working with GraphQL Enums (e.g. with HotChocolate .NET) the Json should use SCREAMING_CASE for the values.
                    //You can customize/override this with [EnumMember()] attributes, but this simplifies when the names should simply match!
                    var namingPolicy = defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableScreamingCaseEnums)
                        ? new FlurlGraphQLSystemTextJsonScreamingCaseNamingPolicy()
                        : null;

                    graphqlJsonOptions.Converters.Add(new JsonStringEnumMemberConverter(namingPolicy, allowIntegerValues: true));
                }
            }

            return graphqlJsonOptions;
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
            var systemTextJsonGraphQLResult = await graphqlResponse.GetJsonAsync<SystemTextJsonGraphQLResult>().ConfigureAwait(false);
            return CreateGraphQLResponseProcessor(systemTextJsonGraphQLResult);
        }

        /// <summary>
        /// Internal Helper to Create the correct IFlurlGraphQLResponseProcessor based on this Json Serializer and the provided System.Text.Json GraphQL Result.
        /// </summary>
        /// <param name="systemTextJsonGraphQLResult"></param>
        /// <returns></returns>
        internal virtual IFlurlGraphQLResponseProcessor CreateGraphQLResponseProcessor(SystemTextJsonGraphQLResult systemTextJsonGraphQLResult)
            => new FlurlGraphQLSystemTextJsonResponseTransformProcessor(systemTextJsonGraphQLResult.Data, systemTextJsonGraphQLResult.Errors, this);

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
