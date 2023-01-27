using System;

namespace Flurl.Http.GraphQL.Querying
{
    /// <summary>
    /// Interface that can be used to implement deserialization of GraphQL Connection Results defined as Edges
    /// that include Cursor details
    /// </summary>
    public interface IGraphQLEdge
    {
        string Cursor { get; set; }
    }
}
