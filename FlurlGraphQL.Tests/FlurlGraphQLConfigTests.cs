using System;
using System.Threading.Tasks;
using FlurlGraphQL.Querying.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLConfigTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public void TestGlobalConfig()
        {
            var defaultConfig = FlurlGraphQLConfig.DefaultConfig;
            Assert.AreEqual(FlurlGraphQLConfig.DefaultPersistedQueryFieldName, defaultConfig.PersistedQueryPayloadFieldName);
            Assert.IsNull(defaultConfig.NewtonsoftJsonSerializerSettings);

            FlurlGraphQLConfig.ConfigureDefaults(config =>
            {
                config.PersistedQueryPayloadFieldName = "test";
                config.NewtonsoftJsonSerializerSettings = new JsonSerializerSettings()
                {
                    MaxDepth = 99,
                    NullValueHandling = NullValueHandling.Include,
                    TypeNameHandling = TypeNameHandling.None
                };
            });

            var newConfig = FlurlGraphQLConfig.DefaultConfig;
            Assert.IsTrue(newConfig is IFlurlGraphQLConfig);
            Assert.AreEqual("test", newConfig.PersistedQueryPayloadFieldName);
            Assert.AreEqual(99, newConfig.NewtonsoftJsonSerializerSettings.MaxDepth);

            Assert.AreNotEqual(defaultConfig.PersistedQueryPayloadFieldName, newConfig.PersistedQueryPayloadFieldName);
            Assert.AreNotEqual(defaultConfig.NewtonsoftJsonSerializerSettings, newConfig.NewtonsoftJsonSerializerSettings);

            //We need to RESET our Defaults so we don't affect other Unit Tests...
            FlurlGraphQLConfig.ResetDefaults();
        }
    }
}