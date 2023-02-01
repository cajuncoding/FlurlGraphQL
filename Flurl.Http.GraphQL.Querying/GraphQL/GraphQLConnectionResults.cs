using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    public class GraphQLConnectionResults<TResult> : GraphQLPaginatedQueryResults<TResult, IGraphQLCursorPageInfo>, IGraphQLConnectionResults<TResult> 
        where TResult : class
    {
        public GraphQLConnectionResults(IList<TResult> results, int? totalCount = null, IGraphQLCursorPageInfo cursorPageInfo = null)
            : base(results, totalCount, cursorPageInfo)
        {
        }
    }
}
