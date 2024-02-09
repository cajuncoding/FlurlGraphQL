using System.Collections.Generic;

namespace FlurlGraphQL.Querying
{
    public class GraphQLCollectionSegmentResults<TResult> : GraphQLPaginatedQueryResults<TResult, IGraphQLOffsetPageInfo>, IGraphQLCollectionSegmentResults<TResult> 
        where TResult : class
    {
        public GraphQLCollectionSegmentResults(IList<TResult> results, int? totalCount = null, IGraphQLOffsetPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
