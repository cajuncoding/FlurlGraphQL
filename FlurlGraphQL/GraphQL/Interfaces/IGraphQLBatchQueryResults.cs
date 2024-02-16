namespace FlurlGraphQL
{
    public interface IGraphQLBatchQueryResults
    {
        int Count { get; }

        /// <summary>
        /// Simply gets the typed results for the GraphQL query in the batch by it's ordinal index (first query is 0).
        /// If the query in the batch has pagination or other extended data such as total count, this will not return those details.
        /// For that information from a Connection or CollectionSegment use one of the other specific methods such as GetConnectionResults() or GetCollectionSegmentResults() respectively.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IGraphQLQueryResults<TResult> GetResults<TResult>(int index) where TResult : class;

        /// <summary>
        /// Simply gets the typed results for the GraphQL query in the batch by it's operationName (case insensitive).
        /// If the query in the batch has pagination or other extended data such as total count, this will not return those details.
        /// For that information from a Connection or CollectionSegment use one of the other specific methods such as GetConnectionResults() or GetCollectionSegmentResults() respectively.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        IGraphQLQueryResults<TResult> GetResults<TResult>(string operationName) where TResult : class;

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IGraphQLConnectionResults<TResult> GetConnectionResults<TResult>(int index) where TResult : class;

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        IGraphQLConnectionResults<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class;

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IGraphQLCollectionSegmentResults<TResult> GetCollectionSegmentResults<TResult>(int index) where TResult : class;

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        IGraphQLCollectionSegmentResults<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class;
    }
}