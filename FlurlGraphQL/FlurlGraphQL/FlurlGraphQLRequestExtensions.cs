using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Util;
using FlurlGraphQL.ValidationExtensions;
using NullValueHandling = Flurl.NullValueHandling;

namespace FlurlGraphQL
{
    public static class ContextItemKeys
    {
        public const string FlurlGraphQLJsonSerializer = nameof(FlurlGraphQLJsonSerializer);
        public const string NewtonsoftJsonSerializerSettings = nameof(NewtonsoftJsonSerializerSettings);
        public const string SystemTextJsonSerializerSettings = nameof(SystemTextJsonSerializerSettings);
        public const string PersistedQueryPayloadFieldName = nameof(PersistedQueryPayloadFieldName);
    }

    public static class FlurlGraphQLRequestExtensions
    {
        #region ToGraphQLRequest() Internal Helpers...

        private static IFlurlGraphQLRequest ToGraphQLRequest(string url) => new FlurlRequest(url).ToGraphQLRequest();
        private static IFlurlGraphQLRequest ToGraphQLRequest(Uri url) => new FlurlRequest(url).ToGraphQLRequest();
        private static IFlurlGraphQLRequest ToGraphQLRequest(Url url) => new FlurlRequest(url).ToGraphQLRequest();

        public static IFlurlGraphQLRequest ToGraphQLRequest(this IFlurlRequest request)
            => request is FlurlGraphQLRequest graphqlRequest ? graphqlRequest : new FlurlGraphQLRequest(request);

        #endregion

        #region WithGraphQLPersistedQuery()

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLPersistedQuery(this string url, string id) => ToGraphQLRequest(url).WithGraphQLPersistedQuery(id);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLPersistedQuery(this Uri url, string id) => ToGraphQLRequest(url).WithGraphQLPersistedQuery(id);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLPersistedQuery(this Url url, string id) => ToGraphQLRequest(url).WithGraphQLPersistedQuery(id);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLPersistedQuery(this IFlurlRequest request, string id) => request.ToGraphQLRequest().WithGraphQLPersistedQuery(id);


        #endregion

        #region WithGraphQLQuery()...

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLQuery(this string url, string query) => ToGraphQLRequest(url).WithGraphQLQuery(query);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLQuery(this Uri url, string query) => ToGraphQLRequest(url).WithGraphQLQuery(query);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLQuery(this Url url, string query) => ToGraphQLRequest(url).WithGraphQLQuery(query);

        /// <summary>
        /// Initialize the query body for a GraphQL query request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="query"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest WithGraphQLQuery(this IFlurlRequest request, string query) => request.ToGraphQLRequest().WithGraphQLQuery(query);

        #endregion

        #region SetGraphQLVariable() & SetGraphQLVariables()...

        /// <summary>
        /// Set a single variable name and value for dynamic execution of a parameterized GraphQL query.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly help the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariable(this string url, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => ToGraphQLRequest(url).SetGraphQLVariable(name, value, nullValueHandling);

        /// <summary>
        /// Set a single variable name and value for dynamic execution of a parameterized GraphQL query.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly help the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariable(this Uri url, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => ToGraphQLRequest(url).SetGraphQLVariable(name, value, nullValueHandling);

        /// <summary>
        /// Set a single variable name and value for dynamic execution of a parameterized GraphQL query.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly help the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariable(this Url url, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => ToGraphQLRequest(url).SetGraphQLVariable(name, value, nullValueHandling);

        /// <summary>
        /// Set a single variable name and value for dynamic execution of a parameterized GraphQL query.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly help the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariable(this IFlurlRequest request, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => request.ToGraphQLRequest().SetGraphQLVariable(name, value, nullValueHandling);

        /// <summary>
        /// Set multiple variables, as provided by the publicly readable properties of the specified class/object, for dynamic execution of a parameterized GraphQL query.
        /// Often used with anonymous objects such as new { key1 = value1, key2 = value2 }, or Dictionaries; but any class with publicly accessible properties will work.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly helps the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="url"></param>
        /// <param name="variables"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariables<TVariables>(this string url, TVariables variables, NullValueHandling nullValueHandling = NullValueHandling.Remove) where TVariables : class
            => ToGraphQLRequest(url).SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);

        /// <summary>
        /// Set multiple variables, as provided by the publicly readable properties of the specified class/object, for dynamic execution of a parameterized GraphQL query.
        /// Often used with anonymous objects such as new { key1 = value1, key2 = value2 }, or Dictionaries; but any class with publicly accessible properties will work.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly helps the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="uri"></param>
        /// <param name="variables"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariables<TVariables>(this Uri uri, TVariables variables, NullValueHandling nullValueHandling = NullValueHandling.Remove) where TVariables : class
            => ToGraphQLRequest(uri).SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);

        /// <summary>
        /// Set multiple variables, as provided by the publicly readable properties of the specified class/object, for dynamic execution of a parameterized GraphQL query.
        /// Often used with anonymous objects such as new { key1 = value1, key2 = value2 }, or Dictionaries; but any class with publicly accessible properties will work.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly helps the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="url"></param>
        /// <param name="variables"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariables<TVariables>(this Url url, TVariables variables, NullValueHandling nullValueHandling = NullValueHandling.Remove) where TVariables : class
            => ToGraphQLRequest(url).SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);

        /// <summary>
        /// Set multiple variables, as provided by the publicly readable properties of the specified class/object, for dynamic execution of a parameterized GraphQL query.
        /// Often used with anonymous objects such as new { key1 = value1, key2 = value2 }, or Dictionaries; but any class with publicly accessible properties will work.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly helps the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="request"></param>
        /// <param name="variables"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariables<TVariables>(this IFlurlRequest request, TVariables variables, NullValueHandling nullValueHandling = NullValueHandling.Remove) where TVariables : class
            => request.ToGraphQLRequest().SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);

        /// <summary>
        /// Set multiple variables, as provided by the publicly readable properties of the specified class/object, for dynamic execution of a parameterized GraphQL query.
        /// Often used with anonymous objects such as new { key1 = value1, key2 = value2 }, or Dictionaries; but any class with publicly accessible properties will work.
        /// Variables are HIGHLY encouraged over hard coding values in the query as this can significantly helps the GraphQL server to improve the performance of query execution.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="variables"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetGraphQLVariables(this IFlurlRequest request, IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => request.ToGraphQLRequest().SetGraphQLVariables(variables, nullValueHandling);

        #endregion

        #region Configuration Extensions - System.Text.Json Serializer Settings (ONLY Available after an IFlurlRequest is initialized)...

        /// <summary>
        /// Initialize a custom Json Serializer using System.Text.Json, but only for this GraphQL request; isolated from the base FlurlRequest and any other GraphQL Requests.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="systemTextJsonOptions"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJsonSerializerOptions(this IFlurlRequest request, JsonSerializerOptions systemTextJsonOptions)
            => request.ToGraphQLRequest().UseGraphQLSystemTextJsonSerializerOptions(systemTextJsonOptions);

        /// <summary>
        /// Initialize a custom Json Serializer using System.Text.Json, but only for this GraphQL request; isolated from the base FlurlRequest and any other GraphQL Requests.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="systemTextJsonOptions"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest UseGraphQLSystemTextJsonSerializerOptions(this IFlurlGraphQLRequest graphqlRequest, JsonSerializerOptions systemTextJsonOptions)
        {
            if (graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
                flurlGraphQLRequest.GraphQLJsonSerializer = new FlurlGraphQLSystemTextJsonSerializer(systemTextJsonOptions.AssertArgIsNotNull(nameof(systemTextJsonOptions)));

            return graphqlRequest;
        }

        /// <summary>
        /// Set the Persisted Query Payload Field name for only this GraphQL request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="fieldName"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetPersistedQueryPayloadFieldName(this IFlurlRequest request, string fieldName)
            => request.ToGraphQLRequest().SetPersistedQueryPayloadFieldName(fieldName);

        /// <summary>
        /// Set the Persisted Query Payload Field name for only this GraphQL request.
        /// </summary>
        /// <param name="graphqlRequest"></param>
        /// <param name="fieldName"></param>
        /// <returns>Returns an IFlurlGraphQLRequest for ready to chain for further initialization or execution.</returns>
        public static IFlurlGraphQLRequest SetPersistedQueryPayloadFieldName(this IFlurlGraphQLRequest graphqlRequest, string fieldName)
        {
            if(graphqlRequest is FlurlGraphQLRequest flurlGraphQLRequest)
                flurlGraphQLRequest.PersistedQueryPayloadFieldName = fieldName.AssertArgIsNotNullOrBlank(nameof(fieldName));
            
            return graphqlRequest;
        }

        #endregion

        #region PostGraphQLQueryAsnc()...

        /// <summary>
        /// Execute the GraphQL Query as a POST request (Strongly Recommended vs GET), along with initialized variables, with the GraphQL Server specified by the original Url.
        /// If no GraphQL Query has been specified via WithGraphQLQuery() then an InvalidOperationException will be thrown; otherwise
        /// any other errors returned by the GraphQL server will result in a FlurlGraphQLException being thrown containing all relevant details about the errors.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns an async IFlurlGraphQLResponse ready to be processed by various ReceiveGraphQL*() methods to handle the results based on the type of query.</returns>
        public static Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync(this IFlurlRequest request, CancellationToken cancellationToken = default)
            => request.ToGraphQLRequest().PostGraphQLQueryAsync<object>(null, cancellationToken: cancellationToken);

        /// <summary>
        /// STRONGLY DISCOURAGED -- Execute the GraphQL Query as a GET request, along with initialized variables, with the GraphQL Server specified by the original Url.
        /// This is Strongly Discouraged as POST requests are much more robust. But this is provided for edge cases where GET requests must be used.
        /// If no GraphQL Query has been specified via WithGraphQLQuery() then an InvalidOperationException will be thrown; otherwise
        /// any other errors returned by the GraphQL server will result in a FlurlGraphQLException being thrown containing all relevant details about the errors.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns an async IFlurlGraphQLResponse ready to be processed by various ReceiveGraphQL*() methods to handle the results based on the type of query.</returns>
        public static Task<IFlurlGraphQLResponse> GetGraphQLQueryAsync(this IFlurlRequest request, CancellationToken cancellationToken = default)
            => request.ToGraphQLRequest().GetGraphQLQueryAsync<object>(null, cancellationToken: cancellationToken);


        #endregion
    }
}
