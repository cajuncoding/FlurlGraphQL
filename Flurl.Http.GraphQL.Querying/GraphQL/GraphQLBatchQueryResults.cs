using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Querying
{
    /// <summary>
    /// Internal class for handling the processing of Operation Results within GraphQLBatchQueryResults
    /// </summary>
    internal class GraphQLQueryOperationResult
    {
        public GraphQLQueryOperationResult(string operationName, JObject resultJson)
        {
            OperationName = operationName.AssertArgIsNotNullOrBlank(nameof(operationName));
            ResultJson = resultJson.AssertArgIsNotNull(nameof(resultJson));
        }

        public string OperationName { get; }
        public JObject ResultJson { get; }

        private object _cachedParsedResults = null;

        public IGraphQLQueryResults<TResult> GetParsedResults<TResult>() where TResult : class
        {
            if (ResultJson == null)
                return default;

            if (_cachedParsedResults is IGraphQLQueryResults<TResult> typedResult)
                return typedResult;

            var parsedResult = ResultJson.ParseJsonToGraphQLResultsInternal<TResult>();
            _cachedParsedResults = parsedResult;
            
            return parsedResult;
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

        internal GraphQLBatchQueryResults(IReadOnlyList<GraphQLQueryOperationResult> queryOperationResults)
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

            var parsedResults = _queryOperationResults[index]?.GetParsedResults<TResult>();
            return parsedResults;
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
        {
            operationName.AssertArgIsNotNullOrBlank(nameof(operationName));
            
            var parsedResults = _queryOperationResultLookup[operationName]
                .FirstOrDefault()
                ?.GetParsedResults<TResult>();

            return parsedResults;
        }

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(int index) where TResult : class
            => ToGraphQLConnectionResultsInternal(GetResults<TResult>(index));

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection operation compatible with the formalized Relay specification for Cursor Paging.
        /// See: https://relay.dev/graphql/connections.htm
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class
            => ToGraphQLConnectionResultsInternal(GetResults<TResult>(operationName));

        private IGraphQLQueryConnectionResult<TResult> ToGraphQLConnectionResultsInternal<TResult>(IGraphQLQueryResults<TResult> results) where TResult : class
        {
            return results is IGraphQLQueryConnectionResult<TResult> connectionResults
                ? connectionResults
                : new GraphQLQueryConnectionResult<TResult>(results.GetResultsInternal(), results.TotalCount);
        }

        /// <summary>
        /// Gets the results for the batch query by its ordinal index (first query is 0), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(int index) where TResult : class
        {
            if (GetConnectionResults<TResult>(index) is GraphQLQueryConnectionResult<TResult> connectionResults)
                return ToCollectionSegmentResultsInternal(connectionResults);

            return null;
        }

        /// <summary>
        /// Gets the results for the batch query by its operationName (case insensitive), along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class
            => ToCollectionSegmentResultsInternal(GetConnectionResults<TResult>(operationName));

        private IGraphQLQueryCollectionSegmentResult<TResult> ToCollectionSegmentResultsInternal<TResult>(IGraphQLQueryConnectionResult<TResult> connectionResults) 
            where TResult : class
        {
            if(connectionResults == null)
                return null;

            var pageInfo = connectionResults.PageInfo;

            return new GraphQLQueryCollectionSegmentResult<TResult>(
                connectionResults.GetResultsInternal(),
                connectionResults.TotalCount,
                new GraphQLOffsetPageInfo(hasNextPage: pageInfo?.HasNextPage, hasPreviousPage: pageInfo?.HasPreviousPage)
            );
        }
    }
}
