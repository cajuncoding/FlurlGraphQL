using Flurl;
using Flurl.Http;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLRequest : IFlurlRequest
    {
        GraphQLQueryType GraphQLQueryType { get; }
        string GraphQLQuery { get; }
        IReadOnlyDictionary<string, object> GraphQLVariables { get; }
        IFlurlGraphQLJsonSerializer GraphQLJsonSerializer { get; }
        IFlurlGraphQLRequest SetGraphQLVariable(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetGraphQLVariables(object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetGraphQLVariables(IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);

        IReadOnlyDictionary<string, object> ContextBag { get; }
        IFlurlGraphQLRequest SetContextItem(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetContextItems(object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetContextItems(IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);

        object GetGraphQLVariable(string name);
        IFlurlGraphQLRequest ClearGraphQLVariables();
        IFlurlGraphQLRequest RemoveGraphQLVariable(string name);
        IFlurlGraphQLRequest WithGraphQLQuery(string query, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest WithGraphQLPersistedQuery(string id, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest ClearGraphQLQuery();
        IFlurlGraphQLRequest Clone();

        /// <summary>
        /// Execute the GraphQL query with the Server using POST request (Strongly Recommended vs Get).
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="variables"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns></returns>
        Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync<TVariables>(
            TVariables variables, 
            CancellationToken cancellationToken = default, 
            NullValueHandling nullValueHandling = NullValueHandling.Remove
        ) where TVariables : class;

        /// <summary>
        /// STRONGLY DISCOURAGED -- Execute the GraphQL query with the Server using GET request.
        /// This is Strongly Discouraged as POST requests are much more robust. But this is provided for edge cases where GET requests must be used.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="variables"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns></returns>
        Task<IFlurlGraphQLResponse> GetGraphQLQueryAsync<TVariables>(
            TVariables variables,
            CancellationToken cancellationToken = default,
            NullValueHandling nullValueHandling = NullValueHandling.Remove
        ) where TVariables : class;
    }
}