﻿using System.Collections.Generic;

namespace FlurlGraphQL
{
    public interface IFlurlGraphQLResponseProcessor
    {
        object Data { get; }
        IReadOnlyList<GraphQLError> Errors { get; }
        IReadOnlyDictionary<string, object> ContextBag { get; }

        IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class;

        IGraphQLBatchQueryResults LoadBatchQueryResults();

        string GetErrorContent();
    }
}