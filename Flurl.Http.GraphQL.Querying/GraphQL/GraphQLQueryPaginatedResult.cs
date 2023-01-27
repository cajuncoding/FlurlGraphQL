using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    /// <summary>
    /// BBernard
    /// Result object when querying for Nodes (not Edges) from GraphQL. For the vast majority of Maestro Use cases
    /// we do not need to know the actual Cursor values for individual results; therefore Edges are overkill and
    /// require more complex queries as well as processing of results.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TPageInfo"></typeparam>
    //TODO: EXTRACT GraphQLQueryPaginatedResult to Interface!!!
    public class GraphQLQueryPaginatedResult<TResult, TPageInfo> : GraphQLQueryResults<TResult>
        where TPageInfo : class
    {
        public GraphQLQueryPaginatedResult(IList<TResult> results, int? totalCount = null, TPageInfo cursorPageInfo = null)
            : base(results)
        {
            //NOTE: The TotalCount and PageInfo are both optional and may be null
            TotalCount = totalCount;
            PageInfo = cursorPageInfo;
        }

        public int? TotalCount { get; }
        public TPageInfo PageInfo { get; }

        public bool HasPageInfo() => PageInfo != null;
        public bool HasTotalCount() => TotalCount != null;
    }
}
