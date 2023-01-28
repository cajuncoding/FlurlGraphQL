﻿namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLQueryPaginatedResult<out TResult, out TPageInfo> : IGraphQLQueryResults<TResult>
        where TResult : class
        where TPageInfo : class
    {
        int? TotalCount { get; }
        TPageInfo PageInfo { get; }
        bool HasPageInfo();
        bool HasTotalCount();
    }
}