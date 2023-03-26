using System;
using System.Collections;

namespace FlurlGraphQL.Querying
{
    internal class GraphQLTypeCache
    {
        public static readonly Type ICollection = typeof(ICollection);
        public static readonly Type IGraphQLQueryResultsType = typeof(IGraphQLQueryResults<>);
        public static readonly Type IGraphQLConnectionResultsType = typeof(IGraphQLConnectionResults<>);
        public static readonly Type IGraphQLCollectionSegmentResultsType = typeof(IGraphQLCollectionSegmentResults<>);
    }
}
