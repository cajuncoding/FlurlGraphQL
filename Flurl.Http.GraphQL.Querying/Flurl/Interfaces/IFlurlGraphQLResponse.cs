namespace Flurl.Http.GraphQL.Querying
{
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest OriginalGraphQLRequest { get; }
        string GraphQLQuery { get; }
    }
}