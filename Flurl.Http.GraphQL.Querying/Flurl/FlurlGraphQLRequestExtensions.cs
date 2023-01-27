using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Util;
using System.Net;

namespace Flurl.Http.GraphQL.Querying
{
    public static class FlurlGraphQLRequestExtensions
    {
        public static FlurlGraphQLRequest ToGraphQLRequest(this IFlurlRequest request)
            => request is FlurlGraphQLRequest graphqlRequest
                ? graphqlRequest
                : new FlurlGraphQLRequest(request);

        public static IFlurlRequest WithGraphQLQuery(this string url, string query) => new FlurlRequest(url).ToGraphQLRequest().WithGraphQLQuery(query);

        public static IFlurlRequest WithGraphQLQuery(this Uri url, string query) => new FlurlRequest(url).ToGraphQLRequest().WithGraphQLQuery(query);

        public static IFlurlRequest WithGraphQLQuery(this Url url, string query) => new FlurlRequest(url).ToGraphQLRequest().WithGraphQLQuery(query);

        public static IFlurlRequest SetGraphQLVariables<TVariables>(this IFlurlRequest request, TVariables variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            where TVariables : class
            => request.ToGraphQLRequest().SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);

        public static IFlurlRequest SetGraphQLVariables(this IFlurlRequest request, IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => request.ToGraphQLRequest().SetGraphQLVariables(variables, nullValueHandling);

        public static IFlurlRequest SetGraphQLVariable(this IFlurlRequest request, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => request.ToGraphQLRequest().SetGraphQLVariable(name, value, nullValueHandling);

        public static Task<IFlurlResponse> PostGraphQLQueryAsync(this IFlurlRequest request, CancellationToken cancellationToken = default)
            => request.ToGraphQLRequest().PostGraphQLQueryAsync<object>(null, cancellationToken: cancellationToken);
 
    }
}
