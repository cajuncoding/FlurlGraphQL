using System;
using Flurl.Http.Configuration;
using FlurlGraphQL.ReflectionConstants;
using FlurlGraphQL.ReflectionExtensions;

namespace FlurlGraphQL.JsonProcessing
{
    internal static class FlurlGraphQLJsonSerializerFactory
    {
        private delegate IFlurlGraphQLJsonSerializer JsonSerializerFactoryDelegate(ISerializer flurlSerializer);

        private static Lazy<JsonSerializerFactoryDelegate> CreateNewtonsoftJsonSerializerFromFlurlSerializerLazy { get; } = new Lazy<JsonSerializerFactoryDelegate>(() =>
        {
            //Ensure that our FlurlGraphQL.Newtonsoft assembly is loaded (since it is dynamically accessed and may not yet be initialized)...
            AppDomain.CurrentDomain.ForceLoadAssemblies(NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftAssemblyName);

            var newtonsoftType = AppDomain.CurrentDomain.FindType(
                NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftJsonSerializerClassName,
                assemblyName: NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftAssemblyName,
                namespaceName: FlurlGraphQLConstants.JsonProcessingNamespace
            );

            return newtonsoftType.CreateDelegateForMethod<JsonSerializerFactoryDelegate>(NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftJsonSerializerFactoryMethodName);
        });

        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlJsonSerializer)
        {
            //If we have a valid IFlurlGraphQLJsonSerializer then we are good otherwise we will try to reverse engineer the Flurl Serializer to create one...
            if (flurlJsonSerializer is IFlurlGraphQLJsonSerializer flurlGraphQLSerializer)
                return flurlGraphQLSerializer;

            //Attempt to brute force detect what kind of core Flurl Serializer is in use and reach under the hood to get the Settings/Options and 
            //  instantiate a valid IFlurlGraphQLJsonSerializer matching the Json parsing being used (e.g. System.Text.Json vs Newtonsoft.Json)...
            var flurlSerializerTypeName = flurlJsonSerializer.GetType().Name;
            switch (flurlJsonSerializer.GetType().Name)
            {
                case FlurlConstants.SystemTextJsonSerializerClassName: return CreateSystemTextJsonSerializer(flurlJsonSerializer);
                case FlurlConstants.NewtonsoftJsonSerializerClassName: return CreateNewtonsoftJsonSerializer(flurlJsonSerializer);
                default: throw new InvalidOperationException($"The current Flurl Json Serializer of type [{flurlSerializerTypeName}] is not supported by FlurlGraphQL; an instance of DefaultJsonSerializer or NewtonsoftJsonSerializer is expected.");
            }
        }

        private static IFlurlGraphQLJsonSerializer CreateSystemTextJsonSerializer(ISerializer flurlJsonSerializer)
            => FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(flurlJsonSerializer);

        //Because Newtonsoft is now extracted into it's own Library that may be optionally included we don't know at compile time
        // if it is loaded and therefore must use runtime reflection to safely initialize it.
        //NOTE: WE will throw a runtime exception if not available because that means that something is mis-configured and not initialized
        //      to support the use of Newtonsoft Json.
        private static IFlurlGraphQLJsonSerializer CreateNewtonsoftJsonSerializer(ISerializer flurlJsonSerializer)
            => CreateNewtonsoftJsonSerializerFromFlurlSerializerLazy.Value?.Invoke(flurlJsonSerializer)
                ?? throw new InvalidOperationException(
                    $"FlurlGraphQL Newtonsoft Json serialization could not be initialized; failed to load the Newtonsoft Json dependency [{NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftJsonSerializerClassName}]. " +
                            $"This is likely due to missing reference(s) for the [{NewtonsoftJsonConstants.FlurlGraphQLNewtonsoftAssemblyName}] library which is required for Newtonsoft Json support."
                );
    }
}
