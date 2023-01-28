using System;

namespace Flurl.Http.GraphQL.Querying
{
    public static class GraphQLConnectionArgs
    {
        //Connection/Cursor Paging Args
        public const string First = "first";
        public const string After = "after";
        public const string Last = "last";
        public const string Before = "before";
    }

    public static class GraphQLCollectionSegmentArgs
    {
        //Collection Segment/Offset Paging Args
        public const string Skip = "skip";
        public const string Take = "take";
    }

    public static class GraphQLFields
    {
        //Paginated Results Elements
        public const string Nodes = "nodes"; //Cursor Paging Results Node
        public const string Items = "items"; //Offset Paging Results Node
        public const string Node = "node";
        public const string Edges = "edges";
        public const string Cursor = "cursor";
        public const string TotalCount = "totalCount";
        public const string PageInfo = "pageInfo";
        public const string Data = "data";
        public const string Errors = "errors";
    }
}
