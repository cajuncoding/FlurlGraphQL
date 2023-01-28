namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLQueryConnectionResult<out TResult> : IGraphQLQueryPaginatedResult<TResult, IGraphQLCursorPageInfo>
        where TResult : class
    {
    }
}