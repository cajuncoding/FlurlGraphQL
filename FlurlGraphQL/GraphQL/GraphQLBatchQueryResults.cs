using System;
using System.Collections.Generic;
using System.Linq;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    /// <summary>
    /// Internal class for handling the processing of Operation Results within GraphQLBatchQueryResults
    /// </summary>
    public class GraphQLQueryOperationResult
    {
        internal GraphQLQueryOperationResult(string operationName, IFlurlGraphQLResponseProcessor responseProcessor)
        {
            OperationName = operationName.AssertArgIsNotNullOrBlank(nameof(operationName));
            _graphQLResponseProcessor = responseProcessor.AssertArgIsNotNull(nameof(responseProcessor));
        }

        private readonly IFlurlGraphQLResponseProcessor _graphQLResponseProcessor;
        private object _cachedParsedResults;
        private readonly object _padLock = new object();

        public string OperationName { get; }

        public IGraphQLQueryResults<TResult> GetParsedResults<TResult>() where TResult : class
        {
            if (_cachedParsedResults is IGraphQLQueryResults<TResult> typedResult)
                return typedResult;

            lock (_padLock)
            {
                var parsedResult = _graphQLResponseProcessor.LoadTypedResults<TResult>(this.OperationName);
                _cachedParsedResults = parsedResult;
                
                return parsedResult;
            }
        }
    }

    /// <summary>
    /// Provides Typed Access to multiple Query Results from a Batch Query.
    /// </summary>
    public class GraphQLBatchQueryResults : IGraphQLBatchQueryResults
    {
        private readonly IReadOnlyList<GraphQLQueryOperationResult> _queryOperationResults;
        private readonly ILookup<string, GraphQLQueryOperationResult> _queryOperationResultLookup;

        public int Count => _queryOperationResults.Count;

        public GraphQLBatchQueryResults(IReadOnlyList<GraphQLQueryOperationResult> queryOperationResults)
        {
            _queryOperationResults = queryOperationResults ?? new List<GraphQLQueryOperationResult>().AsReadOnly();
            
            _queryOperationResultLookup = _queryOperationResults.ToLookup(
                r => r.OperationName, 
                r => r, 
                StringComparer.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Simply gets the typed results for the GraphQL query in the batch by it's ordinal index (first query is 0).
        /// If the query in the batch has pagination or other extended data such as total count, this will not return those details.
        /// For that information from a Connection or CollectionSegment use one of the other specific methods such as GetConnectionResults() or GetCollectionSegmentResults() respectively.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public IGraphQLQueryResults<TResult> GetResults<TResult>(int index) where TResult : class
        {
            if(index < 0 || index > (_queryOperationResults.Count - 1))
                throw new ArgumentOutOfRangeException(nameof(index));

            return _queryOperationResults[index]?.GetParsedResults<TResult>();
        }

        /// <summary>
        /// Simply gets the typed results for the GraphQL query in the batch by it's operationName (case insensitive).
        /// If the query in the batch has pagination or other extended data such as total count, this will not return those details.
        /// For that information from a Connection or CollectionSegment use one of the other specific methods such as GetConnectionResults() or GetCollectionSegmentResults() respectively.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLQueryResults<TResult> GetResults<TResult>(string operationName) where TResult : class
            => _queryOperationResultLookup[operationName.AssertArgIsNotNullOrBlank(nameof(operationName))]
                .FirstOrDefault()
                ?.GetParsedResults<TResult>();

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLConnectionResults<TResult> GetConnectionResults<TResult>(int index) where TResult : class
            => GetResults<TResult>(index).ToGraphQLConnectionResultsInternal();

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLConnectionResults<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class
            => GetResults<TResult>(operationName).ToGraphQLConnectionResultsInternal();

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLCollectionSegmentResults<TResult> GetCollectionSegmentResults<TResult>(int index) where TResult : class
        => GetConnectionResults<TResult>(index) is GraphQLConnectionResults<TResult> connectionResults
                ? connectionResults.ToCollectionSegmentResultsInternal()
                : null;

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLCollectionSegmentResults<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class
            => GetConnectionResults<TResult>(operationName).ToCollectionSegmentResultsInternal();
    }
}
