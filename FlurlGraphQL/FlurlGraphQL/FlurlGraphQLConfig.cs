using System;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLConfig
    {
        string PersistedQueryPayloadFieldName { get; }
    }

    public sealed class FlurlGraphQLConfig : IFlurlGraphQLConfig
    {
        public const string DefaultPersistedQueryFieldName = "id";

        public string PersistedQueryPayloadFieldName { get; set; }

        public static IFlurlGraphQLConfig DefaultConfig { get; private set; } = new FlurlGraphQLConfig();

        private FlurlGraphQLConfig()
        {
            PersistedQueryPayloadFieldName = DefaultPersistedQueryFieldName;
        }

        private FlurlGraphQLConfig Clone() => new FlurlGraphQLConfig()
        {
            PersistedQueryPayloadFieldName = this.PersistedQueryPayloadFieldName
        };

        /// <summary>
        /// Configure the Default values for Sql Bulk Helpers and Materialized Data Helpers.
        /// </summary>
        /// <param name="configAction"></param>
        public static void ConfigureDefaults(Action<FlurlGraphQLConfig> configAction)
        {
            var newConfig = ((FlurlGraphQLConfig)DefaultConfig).Clone();
            
            configAction
                .AssertArgIsNotNull(nameof(configAction))
                .Invoke(newConfig);
            
            DefaultConfig = newConfig;
        }

        /// <summary>
        /// Reset FlurlGraphQL Configuration to all Default values.
        /// </summary>
        /// <returns></returns>
        public static void ResetDefaults() 
            => DefaultConfig = new FlurlGraphQLConfig();
    }
}
