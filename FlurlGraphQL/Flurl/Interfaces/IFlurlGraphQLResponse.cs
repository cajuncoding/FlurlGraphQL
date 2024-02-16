using Flurl.Http;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest GraphQLRequest { get; }
        IFlurlGraphQLJsonSerializer GraphQLJsonSerializer { get; }
        string GraphQLQuery { get; }
    }
}