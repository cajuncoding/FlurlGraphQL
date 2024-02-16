using System;
using System.Text.Json;
using Flurl.Http.Configuration;
using FlurlGraphQL.ReflectionExtensions;


namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLSystemTextJsonSerializer
    {
        public static IFlurlGraphQLSystemTextJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Options directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonOptions = flurlSerializer.BruteForceGet<JsonSerializerOptions>("_options");

            var graphqlJsonOptions = currentJsonOptions != null
                ? new JsonSerializerOptions(currentJsonOptions) //Clone existing Options if available!
                : new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; //Default Options (always Disable Case Sensitivity; which is enabled by default)

            return new FlurlGraphQLSystemTextJsonSerializer(graphqlJsonOptions);
        }

        public JsonSerializerOptions JsonSerializerOptions { get; protected set; }

        public FlurlGraphQLSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            JsonSerializerOptions = jsonSerializerOptions;
        }

        public string SerializeToJson(object obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);

        public TResult DeserializeGraphQLJsonResults<TResult>()
        {
            //TODO: Correctly initialize any Converters needed for De-serialization...

            throw new NotImplementedException();
        }
    }
}
