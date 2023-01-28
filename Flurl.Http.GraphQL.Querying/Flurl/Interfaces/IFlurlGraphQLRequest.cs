using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flurl.Http.GraphQL.Querying
{
    public interface IFlurlGraphQLRequest : IFlurlRequest
    {
        Dictionary<string, object> GraphQLVariables { get; }
        string GraphQLQuery { get; }
        IFlurlGraphQLRequest SetGraphQLVariables(object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetGraphQLVariables(IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest SetGraphQLVariable(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest ClearGraphQLVariables();
        IFlurlGraphQLRequest RemoveGraphQLVariable(string name);
        IFlurlGraphQLRequest WithGraphQLQuery(string query, NullValueHandling nullValueHandling = NullValueHandling.Remove);
        IFlurlGraphQLRequest ClearGraphQLQuery();

        Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync<TVariables>(TVariables variables, CancellationToken cancellationToken = default, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            where TVariables : class;
    }
}