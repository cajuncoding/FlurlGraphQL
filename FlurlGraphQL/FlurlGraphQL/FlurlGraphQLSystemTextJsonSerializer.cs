using System;
using System.IO;
using System.Text.Json;
using Flurl.Http.Configuration;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLSystemTextJsonSerializer
    {
        //private static Func<DefaultJsonSerializer, JsonSerializerOptions> _optionsFieldDelegate;

        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            //if (!(flurlSerializer is DefaultJsonSerializer flurlSystemTextJsonSerializer))
            //    throw new ArgumentException($"The type [{flurlSerializer.GetType().Name}] is invalid and cannot be used to initialize the System.Text.Json GraphQL Serializer; a valid instance of [{nameof(DefaultJsonSerializer)}] is expected.");

            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Options directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonOptions = flurlSerializer.BruteForceGetFieldValue<JsonSerializerOptions>("_options");

            //if (_optionsFieldDelegate == null)
            //    _optionsFieldDelegate = flurlSystemTextJsonSerializer.GetType().CreateGetFieldDelegate<DefaultJsonSerializer, JsonSerializerOptions>("_options");

            //var currentJsonOptions = _optionsFieldDelegate.Invoke(flurlSystemTextJsonSerializer);

            var graphqlJsonOptions = currentJsonOptions != null
                ? new JsonSerializerOptions(currentJsonOptions) //Clone existing Options if available!
                : new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; //Default Options (always Disable Case Sensitivity; which is enabled by default)

            return new FlurlGraphQLSystemTextJsonSerializer(graphqlJsonOptions);
        }

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
        public virtual IFlurlGraphQLResponseProcessor CreateGraphQLResponseProcessor(IFlurlGraphQLResponse graphqlResponse)
            => FlurlGraphQLSystemTextJsonResponseProcessor.FromFlurlGraphQLResponse(graphqlResponse);

        #region Base Flurl ISerializer implementation...

        public string Serialize(object obj) => FlurlSystemTextJsonSerializer.Serialize(obj);
        public T Deserialize<T>(string s) => FlurlSystemTextJsonSerializer.Deserialize<T>(s);
        public T Deserialize<T>(Stream stream) => FlurlSystemTextJsonSerializer.Deserialize<T>(stream);

        #endregion

    }
}
