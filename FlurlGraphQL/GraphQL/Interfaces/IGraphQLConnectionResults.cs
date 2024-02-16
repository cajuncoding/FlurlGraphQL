namespace FlurlGraphQL
{
    public interface IGraphQLConnectionResults<out TResult> : IGraphQLPaginatedQueryResults<TResult, IGraphQLCursorPageInfo>
        where TResult : class
    {
    }
}