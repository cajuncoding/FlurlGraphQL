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

        public static IFlurlGraphQLConfig DefaultConfig { get; private set; }

        private FlurlGraphQLConfig()
        {
            PersistedQueryPayloadFieldName = DefaultPersistedQueryFieldName;
            ResetDefaults();
        }

        /// <summary>
        /// Configure the Default values for Sql Bulk Helpers and Materialized Data Helpers.
        /// </summary>
        /// <param name="configAction"></param>
        public static void ConfigureDefaults(Action<FlurlGraphQLConfig> configAction)
        {
            configAction.AssertArgIsNotNull(nameof(configAction));
            configAction.Invoke((FlurlGraphQLConfig)DefaultConfig);
        }

        /// <summary>
        /// Reset FlurlGraphQL Configuration to all Default values.
        /// </summary>
        /// <returns></returns>
        public static void ResetDefaults() 
            => DefaultConfig = new FlurlGraphQLConfig();
    }
}
