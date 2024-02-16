using System.Collections.Generic;

namespace FlurlGraphQL
{
    public class GraphQLCollectionSegmentResults<TResult> : GraphQLPaginatedQueryResults<TResult, IGraphQLOffsetPageInfo>, IGraphQLCollectionSegmentResults<TResult> 
        where TResult : class
    {
        public GraphQLCollectionSegmentResults(IReadOnlyList<TResult> results, int? totalCount = null, IGraphQLOffsetPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
