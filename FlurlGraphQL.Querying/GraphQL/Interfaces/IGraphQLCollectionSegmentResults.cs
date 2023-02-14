namespace FlurlGraphQL.Querying
{
    public interface IGraphQLCollectionSegmentResults<out TResult> : IGraphQLPaginatedQueryResults<TResult, IGraphQLOffsetPageInfo>
        where TResult : class
    {
    }
}