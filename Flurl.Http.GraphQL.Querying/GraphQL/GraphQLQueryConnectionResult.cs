using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    public class GraphQLQueryConnectionResult<TResult> : GraphQLQueryPaginatedResult<TResult, IGraphQLCursorPageInfo>, IGraphQLQueryConnectionResult<TResult> 
        where TResult : class
    {
        public GraphQLQueryConnectionResult(IList<TResult> results, int? totalCount = null, IGraphQLCursorPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
