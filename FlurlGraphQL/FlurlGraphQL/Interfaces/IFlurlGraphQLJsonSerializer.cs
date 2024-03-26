using Flurl.Http.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlurlGraphQL
{
    /// <summary>
    /// Serializer Interface for GraphQL. This interface is unique from the base Flurl ISerializer because we must
    ///     customize the Serializer to support the complex Json processing for GraphQL responses which then 
    ///     allows FlurlGraphQL to greatly simplify and flatten the Json responses into simplified data models.
    ///  This is done by adding support to create an IFlurlGraphQLResponseProcessor which encapsulates all processing
    ///     of the Json into Typed models with various capabilities.
    /// 
    /// This class is sub-typed further for specific implementations that are provided for the specialized behavior and logic needed for FlurlGraphQL:
    ///  - IFlurlGraphQLSystemTextJsonSerializer
    ///  - IFlurlGraphQLNewtonsoftJsonSerializer
    /// </summary>
    public interface IFlurlGraphQLJsonSerializer : ISerializer
    {
        Task<IFlurlGraphQLResponseProcessor> CreateGraphQLResponseProcessorAsync(IFlurlGraphQLResponse graphqlResponse);
        IReadOnlyList<GraphQLError> ParseErrorsFromGraphQLExceptionErrorContent(string errorContent);
    }

}
