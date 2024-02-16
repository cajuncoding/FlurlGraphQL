using System;

namespace FlurlGraphQL
{
    public class GraphQLOffsetPageInfo : IGraphQLOffsetPageInfo
    {
        public GraphQLOffsetPageInfo(bool? hasNextPage = null, bool? hasPreviousPage = null)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
        }

        public bool? HasNextPage { get; }
        public bool? HasPreviousPage { get; }

        public static IGraphQLOffsetPageInfo FromCursorPageInfo(IGraphQLCursorPageInfo cursorPageInfo)
        {
            //Convert the Cursor Page Info to Offset Page Info (which is a subset of Cursor paging details)
            return cursorPageInfo == null
                ? new GraphQLOffsetPageInfo()
                : new GraphQLOffsetPageInfo(cursorPageInfo.HasNextPage, cursorPageInfo.HasPreviousPage);
        }
    }
}
