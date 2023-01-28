using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    public class GraphQLQueryCollectionSegmentResult<TResult> : GraphQLQueryPaginatedResult<TResult, IGraphQLOffsetPageInfo>, IGraphQLQueryCollectionSegmentResult<TResult> 
        where TResult : class
    {
        public GraphQLQueryCollectionSegmentResult(IList<TResult> results, int? totalCount = null, IGraphQLOffsetPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
