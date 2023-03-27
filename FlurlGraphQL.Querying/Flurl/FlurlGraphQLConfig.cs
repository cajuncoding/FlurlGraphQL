using System;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying
{
    public interface IFlurlGraphQLConfig
    {
        JsonSerializerSettings NewtonsoftJsonSerializerSettings { get; }
        string PersistedQueryPayloadFieldName { get; }
    }

    public sealed class FlurlGraphQLConfig : IFlurlGraphQLConfig
    {
        public const string DefaultPersistedQueryFieldName = "id";

        private FlurlGraphQLConfig()
        {
            NewtonsoftJsonSerializerSettings = JsonConvert.DefaultSettings?.Invoke();
            PersistedQueryPayloadFieldName = DefaultPersistedQueryFieldName;
        }

        public static IFlurlGraphQLConfig DefaultConfig { get; private set; } = new FlurlGraphQLConfig();

        /// <summary>
        /// Configure the Default values for Sql Bulk Helpers and Materialized Data Helpers.
        /// </summary>
        /// <param name="configAction"></param>
        public static void ConfigureDefaults(Action<FlurlGraphQLConfig> configAction)
        {
            configAction.AssertArgIsNotNull(nameof(configAction));

            var newConfig = new FlurlGraphQLConfig();
            configAction.Invoke(newConfig);
            DefaultConfig = newConfig;
        }

        public static void ResetDefaults()
        {
            DefaultConfig = new FlurlGraphQLConfig();
        }

        public JsonSerializerSettings NewtonsoftJsonSerializerSettings { get; set; }
        public string PersistedQueryPayloadFieldName { get; set; }
    }
}
