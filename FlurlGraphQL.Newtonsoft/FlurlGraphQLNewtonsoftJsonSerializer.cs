using Flurl.Http.Configuration;
using Newtonsoft.Json;
using FlurlGraphQL.ReflectionExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLNewtonsoftJsonSerializer : IFlurlGraphQLNewtonsoftJsonSerializer
    {
        public static IFlurlGraphQLNewtonsoftJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Settings directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonSettings = flurlSerializer.BruteForceGet<JsonSerializerSettings>("_settings");

            var graphqlJsonSettings = currentJsonSettings != null
                ? new JsonSerializerSettings(currentJsonSettings) //Clone existing Options if available!
                : JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings(); ; //Default Options (always Disable Case Sensitivity; which is enabled by default)

            return new FlurlGraphQLNewtonsoftJsonSerializer(graphqlJsonSettings);
        }

        public JsonSerializerSettings JsonSerializerSettings { get; protected set; }

        public FlurlGraphQLNewtonsoftJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        public string SerializeToJson(object obj) => JsonConvert.SerializeObject(obj, JsonSerializerSettings);

        public TResult DeserializeGraphQLJsonResults<TResult>()
        {
            //TODO: Correctly initialize any Converters needed for De-serialization...

            throw new NotImplementedException();
        }
    }
}
