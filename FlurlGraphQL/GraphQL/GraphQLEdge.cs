namespace FlurlGraphQL
{
    /// <summary>
    /// Class that can be used to implement deserialization of GraphQL Connection Results defined as Edges
    /// that include Cursor details, etc.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public class GraphQLEdge<TNode> : IGraphQLEdge<TNode>
    {
        public GraphQLEdge(TNode node, string cursor)
        {
            this.Node = node;
            this.Cursor = cursor;
        }

        public TNode Node { get; }
        
        public string Cursor { get; set; }
    }
}
