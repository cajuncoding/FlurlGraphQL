using System.Reflection;
using FlurlGraphQL.NewtonsoftConstants;

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
            var graphqlJsonSerializer = graphqlResponse.GraphQLRequest.GraphQLJsonSerializer;
            switch (graphqlJsonSerializer)
            {
                case IFlurlGraphQLSystemTextJsonSerializer: return CreateSystemTextJsonResponseProcessor(graphqlResponse);
                case IFlurlGraphQLNewtonsoftJsonSerializer: return CreateNewtonsoftJsonResponseProcessor(graphqlResponse);
                default: throw new InvalidOperationException($"The GraphQL Serializer type [{graphqlJsonSerializer?.GetType().Name ?? "null"}] is invalid; the response cannot be processed.");
            }
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
            //TODO: Factor this out into Shared Helper as it's duplicated now two times....
            var newtonsoftResponseProcessorType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.Equals(GraphQLConstants.NewtonsoftAssemblyName, StringComparison.OrdinalIgnoreCase) ?? false)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.Namespace != null
                    && t.Namespace.Equals(GraphQLConstants.NewtonsoftNamespace, StringComparison.OrdinalIgnoreCase)
                    && t.Name.Equals(GraphQLConstants.NewtonsoftJsonResponseProcessorClassName, StringComparison.OrdinalIgnoreCase)
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
