using System;

namespace FlurlGraphQL
{
    public class GraphQLCursorPageInfo : IGraphQLCursorPageInfo
    {
        public GraphQLCursorPageInfo(string startCursor = null, string endCursor = null, bool? hasNextPage = null, bool? hasPreviousPage = null)
        {
            StartCursor = startCursor;
            EndCursor = endCursor;
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
        }

        public string StartCursor { get; }
        public string EndCursor { get; }
        public bool? HasNextPage { get; }
        public bool? HasPreviousPage { get; }
    }
}
