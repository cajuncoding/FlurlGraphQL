using System;

namespace Flurl.Http.GraphQL.Querying
{
    public static class GraphQLArgs
    {
        //Paging Args
        public const string First = "first";
        public const string After = "after";
        public const string Last = "last";
        public const string Before = "before";
    }

    public static class GraphQLFields
    {
        //Paginated Results Elements
        public const string Nodes = "nodes";
        public const string Node = "node";
        public const string Edges = "edges";
        public const string Cursor = "cursor";
        public const string TotalCount = "totalCount";
        public const string PageInfo = "pageInfo";
        public const string Data = "data";
        public const string Errors = "errors";
    }
}
