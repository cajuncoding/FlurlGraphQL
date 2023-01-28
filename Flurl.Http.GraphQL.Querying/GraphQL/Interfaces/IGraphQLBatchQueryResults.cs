namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLBatchQueryResults
    {
        int Count { get; }
        IGraphQLQueryResults<TResult> GetResults<TResult>(int index) where TResult : class;
        IGraphQLQueryResults<TResult> GetResults<TResult>(string operationName) where TResult : class;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(int index) where TResult : class;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(int index) where TResult : class;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class;
    }
}