namespace FlurlGraphQL.ReflectionConstants
{
    internal static class FlurlGraphQLConstants
    {
        public const string JsonPaginationTypeRewrittenPropertyName = "FlurlGraphQLPaginationType";
    }

    internal static class FlurlConstants
    {
        public const string SystemTextJsonSerializerClassName = "DefaultJsonSerializer";
        public const string NewtonsoftJsonSerializerClassName = "NewtonsoftJsonSerializer";
    }

    internal static class NewtonsoftJsonConstants
    {
        public const string FlurlGraphQLNewtonsoftAssemblyName = "FlurlGraphQL.Newtonsoft";
        public const string FlurlGraphQLNewtonsoftNamespace = "FlurlGraphQL";
        public const string FlurlGraphQLNewtonsoftJsonSerializerClassName = "FlurlGraphQLNewtonsoftJsonSerializer";
        public const string FlurlGraphQLNewtonsoftJsonSerializerFactoryMethodName = "FromFlurlSerializer";

        public const string JsonPropertyAttributeName = "JsonProperty";
        public const string JsonPropertyAttributeNamePropertyName = "PropertyName";
    }

    internal static class SystemTextJsonConstants
    {
        public const string JsonPropertyAttributeName = "JsonPropertyName";
        public const string JsonPropertyAttributeNamePropertyName = "Name";
    }
}
