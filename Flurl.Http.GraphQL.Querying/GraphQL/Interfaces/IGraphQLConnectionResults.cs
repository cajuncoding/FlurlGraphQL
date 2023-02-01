namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLConnectionResults<out TResult> : IGraphQLPaginatedQueryResults<TResult, IGraphQLCursorPageInfo>
        where TResult : class
    {
    }
}