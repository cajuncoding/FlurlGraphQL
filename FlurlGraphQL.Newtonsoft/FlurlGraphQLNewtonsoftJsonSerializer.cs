using System.IO;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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

            //Create the GraphQL Json Serializer options with critical default configuration being enforced!
            var graphqlJsonSettings = CreateDefaultSerializerSettings(currentJsonSettings);

            return new FlurlGraphQLNewtonsoftJsonSerializer(graphqlJsonSettings);
        }

        public static JsonSerializerSettings CreateDefaultSerializerSettings(JsonSerializerSettings originalJsonSettings = null)
        {
            //Clone existing Options if available so we isolate our custom changes needed for GraphQL (e.g. custom String Enum converter, Case-insensitive, etc.)!
            var graphqlJsonSettings = originalJsonSettings != null
                //Clone existing Options if available!
                ? new JsonSerializerSettings(originalJsonSettings)
                //Default Options are used as fallback...
                : JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

            var defaultJsonConfig = FlurlGraphQLConfig.DefaultConfig;

            //For compatibility with FlurlGraphQL v1 behavior using Newtonsoft.Json it is case-insensitive by default but does not use Camel Case.
            //This is also helpful since GraphQL Json (and Json in general) use CamelCase and nearly always mismatch C# Naming Pascal Case standards of C# Class Models, etc...
            //NOTE: WE are operating on a copy of the original Json Settings so this does NOT mutate the core/original settings from Flurl or those specified for the GraphQL request, etc.
            if (defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableCamelCaseSerialization))
                graphqlJsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            //For compatibility with FlurlGraphQL v1 behavior (using Newtonsoft.Json) we need to provide support for String to Enum conversion along with support for enum annotations
            //  via [EnumMember(Value ="CustomName")] annotation (compatible with Newtonsoft.Json). In addition, we now also support [Description("CustomName")] annotation for
            //  easier syntax that is arguably more intuitive to use.
            if (defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableStringEnumHandling))
            {
                //NOTE: For performance we KNOW we need to add this if the original options were not provided (e.g. null)...
                if (originalJsonSettings is null || !originalJsonSettings.Converters.OfType<StringEnumConverter>().Any())
                {
                    //To simplify working with GraphQL Enums (e.g. with HotChocolate .NET) the Json should use SCREAMING_CASE for the values.
                    //You can customize/override this with [EnumMember()] attributes, but this simplifies when the names should simply match!
                    graphqlJsonSettings.Converters.Add(
                        defaultJsonConfig.IsJsonProcessingFlagEnabled(JsonDefaults.EnableScreamingCaseEnums)
                            ? new StringEnumConverter(namingStrategy: new FlurlGraphQLNewtonsoftJsonScreamingCaseNamingStrategy(), allowIntegerValues: true)
                            : new StringEnumConverter()
                    );
                }
            }

            return graphqlJsonSettings;
        }

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
