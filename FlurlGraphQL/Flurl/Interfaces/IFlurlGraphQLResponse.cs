using Flurl.Http;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest GraphQLRequest { get; }
        string GraphQLQuery { get; }
    }
}