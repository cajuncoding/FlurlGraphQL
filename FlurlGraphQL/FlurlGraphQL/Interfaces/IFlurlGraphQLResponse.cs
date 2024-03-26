using Flurl.Http;

namespace FlurlGraphQL
{
    //This class is sub-typed for specific implementations, to provide specialized behavior and logic:
    // - IFlurlGraphQLSystemTextJsonResponse
    // - IFlurlGraphQLNewtonsoftJsonResponse
    public interface IFlurlGraphQLResponse : IFlurlResponse
    {
        IFlurlGraphQLRequest GraphQLRequest { get; }
        IFlurlGraphQLJsonSerializer GraphQLJsonSerializer { get; }
        string GraphQLQuery { get; }
    }
}