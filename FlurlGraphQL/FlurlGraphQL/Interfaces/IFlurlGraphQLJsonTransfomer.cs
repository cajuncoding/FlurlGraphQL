
using FlurlGraphQL.JsonProcessing;

namespace FlurlGraphQL
{
    internal interface IFlurlGraphQLJsonTransfomer<TJsonNode>
    {
        FlurlGraphQLJsonTransformTypeInfo JsonTransformTypeInfo { get; }

        /// <summary>
        /// Transform the GraphQL Json results to flatten/simplify the hierarchy and support simplified/easier model mapping so that domain
        ///     models do not need to be polluted with GraphQL specific things like edges, nodes, items, etc.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        (TJsonNode Json, PaginationType? PaginationType) TransformJsonForSimplifiedGraphQLModelMapping(TJsonNode json);
    }
}