
using FlurlGraphQL.JsonProcessing;

namespace FlurlGraphQL
{
    internal interface IFlurlGraphQLJsonRewriter<TJsonNode>
    {
        FlurlGraphQLJsonRewriterTypeInfo JsonRewriterTypeInfo { get; }

        /// <summary>
        /// Rewrites the GraphQL Json results to flatten/simplify the hierarchy and support simplified/easier model mapping so that domain
        ///     models do not need to be polluted with GraphQL specific things like edges, nodes, items, etc.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        (TJsonNode Json, PaginationType? PaginationType) RewriteJsonForSimplifiedGraphQLModelMapping(TJsonNode json);
    }
}