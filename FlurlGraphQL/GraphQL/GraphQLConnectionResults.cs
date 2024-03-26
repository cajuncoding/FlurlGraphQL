using System.Collections.Generic;

namespace FlurlGraphQL
{
    public class GraphQLConnectionResults<TResult> : GraphQLPaginatedQueryResults<TResult, IGraphQLCursorPageInfo>, IGraphQLConnectionResults<TResult> 
        where TResult : class
    {
        public GraphQLConnectionResults(IReadOnlyList<TResult> results, int? totalCount = null, IGraphQLCursorPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
