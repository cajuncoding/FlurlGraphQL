namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLQueryCollectionSegmentResult<out TResult> : IGraphQLQueryPaginatedResult<TResult, IGraphQLOffsetPageInfo>
        where TResult : class
    {
    }
}