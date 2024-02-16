using System.Reflection;
using Flurl.Http.Configuration;
using FlurlGraphQL.NewtonsoftConstants;

namespace FlurlGraphQL.Flurl
{
    internal static class FlurlGraphQLJsonSerializerFactory
    {
        private delegate IFlurlGraphQLJsonSerializer FlurlGraphQLJsonSerializerFactoryDelegate(ISerializer flurlSerializer);
        private static readonly FlurlGraphQLJsonSerializerFactoryDelegate _createNewtonsoftJsonSerializerFromFlurlSerializer;

        static FlurlGraphQLJsonSerializerFactory()
        {
            _createNewtonsoftJsonSerializerFromFlurlSerializer = InitNewtonsoftJsonSerializerFactoryDelegate();
        }

        public static IFlurlGraphQLJsonSerializer FromFlurlSerializer(ISerializer flurlJsonSerializer)
        {
            var flurlSerializerTypeName = flurlJsonSerializer.GetType().Name;

            switch (flurlJsonSerializer.GetType().Name)
            {
                case GraphQLConstants.FlurlSystemTextJsonSerializerClassName: return CreateSystemTextJsonSerializer(flurlJsonSerializer);
                case GraphQLConstants.FlurlNewtonsoftSerializerClassName:     return CreateNewtonsoftJsonSerializer(flurlJsonSerializer);
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
            => _createNewtonsoftJsonSerializerFromFlurlSerializer?.Invoke(flurlJsonSerializer);

        /// <summary>
        /// Search, find, and compile a Delegate for fast creation of Newtonsoft Json Serializer if the FlurlGraphQL.Newtonsoft library is available to use.
        /// </summary>
        /// <returns></returns>
        static FlurlGraphQLJsonSerializerFactoryDelegate InitNewtonsoftJsonSerializerFactoryDelegate()
        {
            //TODO: Factor this out into Shared Helper as it's duplicated now two times....
            var newtonsoftJsonSerializerType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.Equals(GraphQLConstants.NewtonsoftAssemblyName, StringComparison.OrdinalIgnoreCase) ?? false)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.Namespace != null
                    && t.Namespace.Equals(GraphQLConstants.NewtonsoftNamespace, StringComparison.OrdinalIgnoreCase)
                    && t.Name.Equals(GraphQLConstants.NewtonsoftJsonSerializerClassName, StringComparison.OrdinalIgnoreCase)
                );

            if (newtonsoftJsonSerializerType == null)
                return null;

            var factoryMethodInfo = newtonsoftJsonSerializerType.GetMethod(GraphQLConstants.NewtonsoftJsonSerializerFactoryMethodName, BindingFlags.Static | BindingFlags.Public);

            return factoryMethodInfo != null
                ? Delegate.CreateDelegate(typeof(FlurlGraphQLJsonSerializerFactoryDelegate), null, factoryMethodInfo) as FlurlGraphQLJsonSerializerFactoryDelegate
                : null;
        }

    }
}
