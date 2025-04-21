using System;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    [Flags]
    public enum JsonDefaults
    {
        None = 0, // No settings enabled
        EnableStringEnumHandling = 1 << 0,
        EnableScreamingCaseEnums = 1 << 1,
        /// <summary>
        /// This ONLY applies to System.Text.Json. Case-insensitive matching cannot be disabled with Newtonsoft.Json.
        /// </summary>
        EnableCaseInsensitiveJsonHandling = 1 << 2,
        EnableCamelCaseSerialization = 1 << 3,
        // Combine all to simplify the Common Case!
        EnableAll = EnableStringEnumHandling | EnableScreamingCaseEnums | EnableCaseInsensitiveJsonHandling | EnableCamelCaseSerialization
    }

    public interface IFlurlGraphQLConfig
    {
        string PersistedQueryPayloadFieldName { get; }
        JsonDefaults JsonProcessingDefaults { get; }
        bool IsJsonProcessingFlagEnabled(JsonDefaults jsonFlag);
        FlurlGraphQLConfig Clone();
    }

    public sealed class FlurlGraphQLConfig : IFlurlGraphQLConfig
    {
        public const string DefaultPersistedQueryFieldName = "id";

        public string PersistedQueryPayloadFieldName { get; set; } = DefaultPersistedQueryFieldName;

        public JsonDefaults JsonProcessingDefaults { get; set; } = JsonDefaults.EnableAll;

        public bool IsJsonProcessingFlagEnabled(JsonDefaults jsonFlag) => JsonProcessingDefaults.HasFlag(jsonFlag);

        public static IFlurlGraphQLConfig DefaultConfig { get; private set; } = new FlurlGraphQLConfig();

        private FlurlGraphQLConfig()
        {
        }

        public FlurlGraphQLConfig Clone() => new FlurlGraphQLConfig()
        {
            PersistedQueryPayloadFieldName = this.PersistedQueryPayloadFieldName,
            JsonProcessingDefaults = this.JsonProcessingDefaults
        };

        /// <summary>
        /// Configure the Default values for Sql Bulk Helpers and Materialized Data Helpers.
        /// </summary>
        /// <param name="configAction"></param>
        public static void ConfigureDefaults(Action<FlurlGraphQLConfig> configAction)
        {
            var newConfig = DefaultConfig.Clone();
            
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
