using System;
using System.IO;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLNewtonsoftJsonSerializer : IFlurlGraphQLNewtonsoftJsonSerializer
    {
        //private static Func<NewtonsoftJsonSerializer, JsonSerializerSettings> _settingsFieldDelegate;
        //NOTE: This is DYNAMICALLY Invoked and used at Runtime by the FlurlGraphQLJsonSerializerFactory class.
        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlSerializer)
        {
            //if (!(flurlSerializer is NewtonsoftJsonSerializer flurlNewtonsoftJsonSerializer))
            //    throw new ArgumentException($"The type [{flurlSerializer.GetType().Name}] is invalid and cannot be used to initialize the Newtonsoft.Json GraphQL Serializer; a valid instance of [{nameof(NewtonsoftJsonSerializer)}] is expected.");

            // NOTE: Due to the abstractions of the core Flurl library we cannot access the Json Serializer Settings directly
            //         and therefore must use dynamic instantiation via Reflection (leveraging brute force to access internal Options/Settings)
            //         depending on if the System.Text.Json Serializer is used or if the Newtonsoft Json Serializer is being used.
            var currentJsonSettings = flurlSerializer.BruteForceGetFieldValue<JsonSerializerSettings>("_settings");

            //if (_settingsFieldDelegate == null)
            //    _settingsFieldDelegate = flurlNewtonsoftJsonSerializer.GetType().CreateGetFieldDelegate<NewtonsoftJsonSerializer, JsonSerializerSettings>("_settings");

            //var currentJsonSettings = _settingsFieldDelegate.Invoke(flurlNewtonsoftJsonSerializer);

            var graphqlJsonSettings = currentJsonSettings != null
                ? new JsonSerializerSettings(currentJsonSettings) //Clone existing Options if available!
                : JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings(); //Default Options (always Disable Case Sensitivity; which is enabled by default)

            return new FlurlGraphQLNewtonsoftJsonSerializer(graphqlJsonSettings);
        }

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
        public virtual IFlurlGraphQLResponseProcessor CreateGraphQLResponseProcessor(IFlurlGraphQLResponse graphqlResponse)
            => FlurlGraphQLNewtonsoftJsonResponseProcessor.FromFlurlGraphQLResponse(graphqlResponse);

        #region Base Flurl ISerializer implementation...
        
        public string Serialize(object obj) => FlurlNewtonsoftSerializer.Serialize(obj);
        public T Deserialize<T>(string s) => FlurlNewtonsoftSerializer.Deserialize<T>(s);
        public T Deserialize<T>(Stream stream) => FlurlNewtonsoftSerializer.Deserialize<T>(stream);

        #endregion
    }
}
