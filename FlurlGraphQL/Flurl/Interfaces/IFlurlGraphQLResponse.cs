using Flurl.Http;

namespace FlurlGraphQL.Querying
{
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest GraphQLRequest { get; }
        string GraphQLQuery { get; }
    }
}