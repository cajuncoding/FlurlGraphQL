namespace FlurlGraphQL.Querying
{
    public interface IGraphQLConnectionResults<out TResult> : IGraphQLPaginatedQueryResults<TResult, IGraphQLCursorPageInfo>
        where TResult : class
    {
    }
}