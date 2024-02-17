using System;
using FlurlGraphQL.NewtonsoftConstants;
using FlurlGraphQL.ReflectionExtensions;

namespace FlurlGraphQL
{
    internal static class FlurlGraphQLJsonResponseProcessorFactory
    {

        private delegate IFlurlGraphQLResponseProcessor JsonResponseProcessorFactoryDelegate(IFlurlGraphQLResponse graphqlResponse);

        private static readonly Lazy<JsonResponseProcessorFactoryDelegate> _createNewtonsoftJsonProcessorFromFlurlResponse = new Lazy<JsonResponseProcessorFactoryDelegate>(() =>
            AppDomain.CurrentDomain.FindType(
                ReflectionConstants.NewtonsoftJsonResponseProcessorClassName,
                assemblyName: ReflectionConstants.NewtonsoftAssemblyName,
                namespaceName: ReflectionConstants.NewtonsoftNamespace
            ).CreateDelegateForMethod<JsonResponseProcessorFactoryDelegate>(ReflectionConstants.NewtonsoftJsonResponseProcessorFactoryMethodName)
        );

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
            => _createNewtonsoftJsonProcessorFromFlurlResponse.Value?.Invoke(graphqlResponse);
    }
}
