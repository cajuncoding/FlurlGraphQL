using System;
using System.Reflection;
using FlurlGraphQL.NewtonsoftConstants;
using FlurlGraphQL.ReflectionExtensions;

namespace FlurlGraphQL.Flurl
{
    internal static class FlurlGraphQLJsonResponseProcessorFactory
    {

        private delegate IFlurlGraphQLResponseProcessor FlurlGraphQLJsonResponseProcessorFactoryDelegate(IFlurlGraphQLResponse graphqlResponse);
        private static readonly FlurlGraphQLJsonResponseProcessorFactoryDelegate _createNewtonsoftJsonProcessorFromFlurlResponse;

        static FlurlGraphQLJsonResponseProcessorFactory()
        {
            _createNewtonsoftJsonProcessorFromFlurlResponse = InitNewtonsoftJsonResponseProcessorFactoryDelegate();
        }

        public static IFlurlGraphQLResponseProcessor FromGraphQLFlurlResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            if (graphqlResponse.GraphQLJsonSerializer is IFlurlGraphQLSystemTextJsonSerializer)
                return CreateSystemTextJsonResponseProcessor(graphqlResponse);
            else if (graphqlResponse.GraphQLJsonSerializer is IFlurlGraphQLNewtonsoftJsonSerializer)
                return CreateNewtonsoftJsonResponseProcessor(graphqlResponse);
            else
                throw new InvalidOperationException($"The GraphQL Serializer type [{graphqlResponse.GraphQLJsonSerializer.GetType().Name}] is invalid; the response cannot be processed.");
        }

        private static IFlurlGraphQLResponseProcessor CreateSystemTextJsonResponseProcessor(IFlurlGraphQLResponse graphqlResponse)
            => FlurlGraphQLSystemTextJsonResponseProcessor.FromFlurlGraphQLResponse(graphqlResponse);

        //Because Newtonsoft is now extracted into it's own Library that may be optionally included we don't know at compile time
        // if it is loaded and therefore must use runtime reflection to safely initialize it.
        //NOTE: WE will throw a runtime exception if not available because that means that something is mis-configured and not initialized
        //      to support the use of Newtonsoft Json.
        private static IFlurlGraphQLResponseProcessor CreateNewtonsoftJsonResponseProcessor(IFlurlGraphQLResponse graphqlResponse)
            => _createNewtonsoftJsonProcessorFromFlurlResponse?.Invoke(graphqlResponse);

        /// <summary>
        /// Search, find, and compile a Delegate for fast creation of Newtonsoft Json Serializer if the FlurlGraphQL.Newtonsoft library is available to use.
        /// </summary>
        /// <returns></returns>
        static FlurlGraphQLJsonResponseProcessorFactoryDelegate InitNewtonsoftJsonResponseProcessorFactoryDelegate()
        {
            var newtonsoftResponseProcessorType = AppDomain.CurrentDomain.FindType(
                GraphQLConstants.NewtonsoftJsonResponseProcessorClassName,
                assemblyName: GraphQLConstants.NewtonsoftAssemblyName,
                namespaceName: GraphQLConstants.NewtonsoftNamespace
            );

            if (newtonsoftResponseProcessorType == null)
                return null;

            var factoryMethodInfo = newtonsoftResponseProcessorType.GetMethod(GraphQLConstants.NewtonsoftJsonResponseProcessorFactoryMethodName, BindingFlags.Static | BindingFlags.Public);

            return factoryMethodInfo != null
                ? Delegate.CreateDelegate(typeof(FlurlGraphQLJsonResponseProcessorFactoryDelegate), null, factoryMethodInfo) as FlurlGraphQLJsonResponseProcessorFactoryDelegate
                : null;
        }

    }
}
