namespace Flurl.Http.GraphQL.Querying
{
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest GraphQLRequest { get; }
        string GraphQLQuery { get; }
    }
}