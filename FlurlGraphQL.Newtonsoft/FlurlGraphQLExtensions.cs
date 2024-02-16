using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http.Newtonsoft;

namespace FlurlGraphQL
{
    /// <summary>
    /// Provides legacy shim methods for dynamic support that is now only available with Newtonsoft Json via Flurl.Http.Newtonsoft compatibility library.
    /// </summary>
    public static class FlurlGraphQLExtensions
    {
        public static Task<dynamic> GetJsonAsync(this IFlurlGraphQLResponse graphqlResponse)
            => graphqlResponse.AsFlurlGraphQLResponse()?.BaseFlurlResponse?.GetJsonAsync();

        public static Task<IList<dynamic>> GetJsonListAsync(this IFlurlGraphQLResponse graphqlResponse)
            => graphqlResponse.AsFlurlGraphQLResponse()?.BaseFlurlResponse?.GetJsonListAsync();

        private static FlurlGraphQLResponse AsFlurlGraphQLResponse(this IFlurlGraphQLResponse graphqlResponse)
            => (graphqlResponse as FlurlGraphQLResponse) ?? throw new ArgumentException($"The GraphQL Response is not of the expected type [{nameof(FlurlGraphQLResponse)}].", nameof(graphqlResponse));

    }
}