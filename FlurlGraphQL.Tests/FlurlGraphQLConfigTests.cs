using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLConfigTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public void TestGlobalConfig()
        {
            var defaultConfig = FlurlGraphQLConfig.DefaultConfig;
            Assert.AreEqual(FlurlGraphQLConfig.DefaultPersistedQueryFieldName, defaultConfig.PersistedQueryPayloadFieldName);

            FlurlGraphQLConfig.ConfigureDefaults(config =>
            {
                config.PersistedQueryPayloadFieldName = "test";
            });

            var newConfig = FlurlGraphQLConfig.DefaultConfig;
            Assert.IsTrue(newConfig is IFlurlGraphQLConfig);
            Assert.AreEqual("test", newConfig.PersistedQueryPayloadFieldName);

            Assert.AreNotEqual(defaultConfig.PersistedQueryPayloadFieldName, newConfig.PersistedQueryPayloadFieldName);

            //We need to RESET our Defaults so we don't affect other Unit Tests...
            FlurlGraphQLConfig.ResetDefaults();
        }

        [TestMethod]
        public void TestSystemTextJsonSerializerConfig()
        {
            var graphqlRequest = "No Op Query".WithSettings(s =>
                {
                    s.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions()
                    {
                        MaxDepth = 99,
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                })
                .ToGraphQLRequest();

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLSystemTextJsonSerializer;

            Assert.IsNotNull(graphqlJsonSerializer);
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerOptions.MaxDepth);
            Assert.AreEqual(true, graphqlJsonSerializer.JsonSerializerOptions.WriteIndented);
            Assert.AreEqual(JsonIgnoreCondition.WhenWritingNull, graphqlJsonSerializer.JsonSerializerOptions.DefaultIgnoreCondition);
        }


        [TestMethod]
        public void TestNewtonsoftJsonSerializerConfig()
        {
            var graphqlRequest = "No Op Query".WithSettings(s =>
                {
                    s.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings()
                    {
                        MaxDepth = 99,
                        NullValueHandling = NullValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.None
                    });
                })
                .ToGraphQLRequest();

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLNewtonsoftJsonSerializer;

            Assert.IsNotNull(graphqlJsonSerializer);
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerSettings.MaxDepth);
            Assert.AreEqual(NullValueHandling.Include, graphqlJsonSerializer.JsonSerializerSettings.NullValueHandling);
            Assert.AreEqual(TypeNameHandling.None, graphqlJsonSerializer.JsonSerializerSettings.TypeNameHandling);
        }
    }
}