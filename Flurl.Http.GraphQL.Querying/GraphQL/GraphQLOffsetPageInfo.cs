using System;

namespace Flurl.Http.GraphQL.Querying
{
    //TODO: EXTRACT GraphQLOffsetPageInfo to Interface!!!
    public class GraphQLOffsetPageInfo
    {
        public GraphQLOffsetPageInfo(bool? hasNextPage = null, bool? hasPreviousPage = null)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
        }

        public bool? HasNextPage { get; }
        public bool? HasPreviousPage { get; }
    }
}
