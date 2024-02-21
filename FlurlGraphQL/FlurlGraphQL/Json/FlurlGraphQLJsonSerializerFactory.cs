﻿using System;
using Flurl.Http.Configuration;
using FlurlGraphQL.NewtonsoftConstants;
using FlurlGraphQL.ReflectionExtensions;

namespace FlurlGraphQL
{
    internal static class FlurlGraphQLJsonSerializerFactory
    {
        private delegate IFlurlGraphQLJsonSerializer JsonSerializerFactoryDelegate(ISerializer flurlSerializer);

        private static Lazy<JsonSerializerFactoryDelegate> CreateNewtonsoftJsonSerializerFromFlurlSerializerLazy { get; } = new Lazy<JsonSerializerFactoryDelegate>(() =>
            AppDomain.CurrentDomain.FindType(
                ReflectionConstants.NewtonsoftJsonSerializerClassName,
                assemblyName: ReflectionConstants.NewtonsoftAssemblyName,
                namespaceName: ReflectionConstants.NewtonsoftNamespace
            ).CreateDelegateForMethod<JsonSerializerFactoryDelegate>(ReflectionConstants.NewtonsoftJsonSerializerFactoryMethodName)
        );

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
                case ReflectionConstants.FlurlSystemTextJsonSerializerClassName: return CreateSystemTextJsonSerializer(flurlJsonSerializer);
                case ReflectionConstants.FlurlNewtonsoftJsonSerializerClassName: return CreateNewtonsoftJsonSerializer(flurlJsonSerializer);
                default: throw new InvalidOperationException($"The current Flurl Json Serializer of type [{flurlSerializerTypeName}] is not supported; a DefaultJsonSerializer or NewtonsoftJsonSerializer is expected.");
            }
        }

        private static IFlurlGraphQLJsonSerializer CreateSystemTextJsonSerializer(ISerializer flurlJsonSerializer)
            => FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(flurlJsonSerializer);

        //Because Newtonsoft is now extracted into it's own Library that may be optionally included we don't know at compile time
        // if it is loaded and therefore must use runtime reflection to safely initialize it.
        //NOTE: WE will throw a runtime exception if not available because that means that something is mis-configured and not initialized
        //      to support the use of Newtonsoft Json.
        private static IFlurlGraphQLJsonSerializer CreateNewtonsoftJsonSerializer(ISerializer flurlJsonSerializer)
            => CreateNewtonsoftJsonSerializerFromFlurlSerializerLazy.Value?.Invoke(flurlJsonSerializer);
    }
}