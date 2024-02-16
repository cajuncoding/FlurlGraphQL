//TODO: DELETE IF NOT USED....???

//using Flurl.Http.Configuration;
//using Flurl.Http.Newtonsoft;
//using Newtonsoft.Json;

//namespace FlurlGraphQL
//{
//    public static class FlurlExtensions
//    {
//        /// <summary>
//        /// Shortcut to use NewtonsoftJsonSerializer with this IFlurlClientBuilder.
//        /// </summary>
//        /// <param name="builder">This IFlurlClientBuilder.</param>
//        /// <param name="settings">Optional custom JsonSerializerSettings.</param>
//        /// <returns></returns>
//        public static IFlurlClientBuilder UseNewtonsoftForGraphQL(this IFlurlClientBuilder builder, JsonSerializerSettings settings = null)
//        {
//            var jsonSerializerSettings = settings ?? JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();
//            FlurlGraphQLConfig.RegisterJsonSerializer(new FlurlGraphQLNewtonsoftJsonSerializer(jsonSerializerSettings));

//            return builder;
//        }

//        /// <summary>
//        /// Shortcut to use NewtonsoftJsonSerializer with all FlurlClients registered in this cache.
//        /// </summary>
//        /// <param name="cache">This IFlurlClientCache.</param>
//        /// <param name="settings">Optional custom JsonSerializerSettings.</param>
//        /// <returns></returns>
//        public static IFlurlClientCache UseNewtonsoftForGraphQL(this IFlurlClientCache cache, JsonSerializerSettings settings = null) =>
//            cache.WithDefaults(builder => builder.UseNewtonsoft(settings));
//    }
//}