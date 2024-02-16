namespace FlurlGraphQL
{
    public interface IGraphQLCollectionSegmentResults<out TResult> : IGraphQLPaginatedQueryResults<TResult, IGraphQLOffsetPageInfo>
        where TResult : class
    {
    }
}