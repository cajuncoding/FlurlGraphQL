using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    //TODO: EXTRACT GraphQLQueryCollectionSegmentResult to Interface!!!
    public class GraphQLQueryCollectionSegmentResult<TResult> : GraphQLQueryPaginatedResult<TResult, GraphQLOffsetPageInfo>
    {
        public GraphQLQueryCollectionSegmentResult(IList<TResult> results, int? totalCount = null, GraphQLOffsetPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
