using System;

namespace Flurl.Http.GraphQL.Querying
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
    }
}
