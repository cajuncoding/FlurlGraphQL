using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    //TODO: EXTRACT GraphQLQueryConnectionResult to Interface!!!
    public class GraphQLQueryConnectionResult<TResult> : GraphQLQueryPaginatedResult<TResult, GraphQLCursorPageInfo>
    {
        public GraphQLQueryConnectionResult(IList<TResult> results, int? totalCount = null, GraphQLCursorPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
