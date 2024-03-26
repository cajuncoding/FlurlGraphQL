using System.Collections.Generic;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLResponseProcessor
    {
        TJson GetRawJsonData<TJson>();

        IReadOnlyList<GraphQLError> GetGraphQLErrors();

        IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class;

        IGraphQLBatchQueryResults LoadBatchQueryResults();

        string GetErrorContent();
    }
}