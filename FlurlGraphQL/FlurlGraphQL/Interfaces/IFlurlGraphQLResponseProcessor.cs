using System.Collections.Generic;

namespace FlurlGraphQL
{
    internal interface IFlurlGraphQLResponseProcessor
    {
        object Data { get; }
        IReadOnlyList<GraphQLError> Errors { get; }

        IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class;

        IGraphQLBatchQueryResults LoadBatchQueryResults();

        string GetErrorContent();
    }
}