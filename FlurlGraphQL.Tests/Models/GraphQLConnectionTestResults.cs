using System;
using System.Collections.Generic;
using System.Linq;

namespace FlurlGraphQL.Tests.Models
{
    internal class GraphQLConnectionTestResults<TEntity>
    {
        public GraphQLConnectionTestResults(IReadOnlyList<TEntity> entities, GraphQLCursorPageInfo pageInfo)
        {
            PageInfo = pageInfo;
            Edges = entities.Select(e => new GraphQLEdge<TEntity>(e, Guid.NewGuid().ToString("N"))).ToArray();
        }

        public GraphQLCursorPageInfo PageInfo { get; }
        public GraphQLEdge<TEntity>[] Edges { get; }
    }
}
