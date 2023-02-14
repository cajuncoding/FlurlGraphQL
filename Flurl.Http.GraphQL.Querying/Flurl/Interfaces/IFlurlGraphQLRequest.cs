using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flurl.Http.GraphQL.Querying
{
    public interface IFlurlGraphQLRequest : IFlurlRequest
    {
        string GraphQLQuery { get; }
        IReadOnlyDictionary<string, object> GraphQLVariables { get; }
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
        IFlurlGraphQLRequest ClearGraphQLQuery();
        IFlurlGraphQLRequest Clone();

        Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync<TVariables>(TVariables variables, CancellationToken cancellationToken = default, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            where TVariables : class;
    }
}