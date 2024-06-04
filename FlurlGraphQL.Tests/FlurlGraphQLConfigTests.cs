using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.JsonProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLConfigTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public void TestGlobalGraphQLConfig()
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
        public void TestSystemTextJsonSerializerFlurlRequestLevelConfig()
        {
            var graphqlRequest = "http://www.no-op-url.com/".WithSettings(s =>
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
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLSystemTextJsonSerializer));
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerOptions.MaxDepth);
            Assert.AreEqual(true, graphqlJsonSerializer.JsonSerializerOptions.WriteIndented);
            Assert.AreEqual(JsonIgnoreCondition.WhenWritingNull, graphqlJsonSerializer.JsonSerializerOptions.DefaultIgnoreCondition);
        }

        //TODO: Update to execute Full Request so that clients are actually initialized to fix this Test!
        [Ignore]
        [TestMethod]
        public void TestSystemTextJsonSerializerFlurlGlobalConfig()
        {
            FlurlHttp.Clients.WithDefaults(builder =>
                builder.WithSettings(s =>
                {
                    s.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions()
                    {
                        MaxDepth = 99,
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                })
            );

            var graphqlRequest = "http://www.no-op-url.com/".AppendPathSegment("graphql").WithGraphQLQuery("No Op Query");

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLSystemTextJsonSerializer;
            Assert.IsNotNull(graphqlJsonSerializer);
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLSystemTextJsonSerializer));
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerOptions.MaxDepth);
            Assert.AreEqual(true, graphqlJsonSerializer.JsonSerializerOptions.WriteIndented);
            Assert.AreEqual(JsonIgnoreCondition.WhenWritingNull, graphqlJsonSerializer.JsonSerializerOptions.DefaultIgnoreCondition);
        }

        [TestMethod]
        public void TestSystemTextJsonSerializerGraphQLSpecificRequestLevelConfig()
        {
            var graphqlRequest = "http://www.no-op-url.com/".WithSettings(s =>
                {
                    //Initialize core Flurl as Newtonsoft (opposite of what we are trying to test)...
                    s.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings());
                })
                .ToGraphQLRequest()
                //THEN Override GraphQL with System.Text.Json...
                .UseGraphQLSystemTextJson();

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLSystemTextJsonSerializer;
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLSystemTextJsonSerializer));
        }

        [TestMethod]
        public void TestNewtonsoftJsonSerializerFlurlRequestLevelConfig()
        {
            var graphqlRequest = "http://www.no-op-url.com/".WithSettings(s =>
                {
                    s.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings()
                    {
                        MaxDepth = 99,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.None
                    });
                })
                .ToGraphQLRequest();

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLNewtonsoftJsonSerializer;

            Assert.IsNotNull(graphqlJsonSerializer);
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLNewtonsoftJsonSerializer));
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerSettings.MaxDepth);
            Assert.AreEqual(Newtonsoft.Json.NullValueHandling.Include, graphqlJsonSerializer.JsonSerializerSettings.NullValueHandling);
            Assert.AreEqual(TypeNameHandling.None, graphqlJsonSerializer.JsonSerializerSettings.TypeNameHandling);
        }

        //TODO: Update to execute Full Request so that clients are actually initialized to fix this Test!
        [Ignore]
        [TestMethod]
        public void TestNewtonsoftJsonSerializerFlurlGlobalConfig()
        {
            FlurlHttp.Clients.UseNewtonsoft(new JsonSerializerSettings()
            {
                MaxDepth = 99,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                TypeNameHandling = TypeNameHandling.None
            });

            var graphqlRequest = "http://www.no-op-url.com/".AppendPathSegment("graphql").WithGraphQLQuery("No Op Query");

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLNewtonsoftJsonSerializer;
            Assert.IsNotNull(graphqlJsonSerializer);
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLNewtonsoftJsonSerializer));
            Assert.AreEqual(99, graphqlJsonSerializer.JsonSerializerSettings.MaxDepth);
            Assert.AreEqual(Newtonsoft.Json.NullValueHandling.Include, graphqlJsonSerializer.JsonSerializerSettings.NullValueHandling);
            Assert.AreEqual(TypeNameHandling.None, graphqlJsonSerializer.JsonSerializerSettings.TypeNameHandling);
        }

        [TestMethod]
        public void TestNewtonsoftJsonSerializerGraphQLSpecificRequestLevelConfig()
        {
            var graphqlRequest = "http://www.no-op-url.com/".WithSettings(s =>
                {
                    //Initialize core Flurl as Newtonsoft (opposite of what we are trying to test)...
                    s.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions());
                })
                .ToGraphQLRequest()
                //THEN Override GraphQL with Newtonsoft.Json...
                .UseGraphQLNewtonsoftJson();

            var graphqlJsonSerializer = graphqlRequest.GraphQLJsonSerializer as FlurlGraphQLNewtonsoftJsonSerializer;
            Assert.IsInstanceOfType(graphqlJsonSerializer, typeof(FlurlGraphQLNewtonsoftJsonSerializer));
        }
    }
}