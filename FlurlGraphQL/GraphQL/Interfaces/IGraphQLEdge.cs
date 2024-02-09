using System;

namespace FlurlGraphQL.Querying
{
    /// <summary>
    /// Interface that can be used to implement deserialization of GraphQL Connection Results defined as Edges
    /// that include Cursor details, and potentially other properties as extensions of the Edge in the GraphQL API.
    /// </summary>
    public interface IGraphQLEdge
    {
        /// <summary>
        /// Cursor property of the Edge. Must be writable so that it can be directly set during de-serialization;
        ///     constructor settings is not supported on custom types during deserialization.
        /// </summary>
        string Cursor { get; set; }
    }

    /// <summary>
    /// Interface that can be used to implement deserialization of GraphQL Connection Results defined as Edges
    /// that include generic Node as well as Cursor details, and potentially other properties as extensions of the Edge in the GraphQL API.
    /// </summary>
    public interface IGraphQLEdge<out TNode> : IGraphQLEdge
    {
        TNode Node { get; }
    }
}
