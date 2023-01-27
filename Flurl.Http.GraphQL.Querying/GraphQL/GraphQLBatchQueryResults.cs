using System;
using System.Collections.Generic;
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

        public GraphQLQueryResults<TResult> GetParsedResults<TResult>() where TResult : class
        {
            if (ResultJson == null)
                return default;

            if (_cachedParsedResults is GraphQLQueryResults<TResult> typedResult)
                return typedResult;

            var parsedResult = ResultJson.ConvertToGraphQLResultsInternal<TResult>();
            _cachedParsedResults = parsedResult;
            
            return parsedResult;
        }
    }

    //TODO: EXTRACT GraphQLBatchQueryResults to Interface!!!
    /// <summary>
    /// Provides Typed Access to multiple Query Results from a Batch Query.
    /// </summary>
    public class GraphQLBatchQueryResults
    {
        private readonly IReadOnlyList<GraphQLQueryOperationResult> _queryOperationResults;
        private readonly ILookup<string, GraphQLQueryOperationResult> _queryOperationResultLookup;

        public int Count => _queryOperationResults.Count;

        internal GraphQLBatchQueryResults(IReadOnlyList<GraphQLQueryOperationResult> queryOperationResults)
        {
            _queryOperationResults = queryOperationResults ?? new List<GraphQLQueryOperationResult>().AsReadOnly();
            _queryOperationResultLookup = _queryOperationResults.ToLookup(r => r.OperationName, r => r);
        }

        public GraphQLQueryResults<TResult> GetResults<TResult>(int index) where TResult : class
        {
            if(index < 0 || index > (_queryOperationResults.Count - 1))
                throw new ArgumentOutOfRangeException(nameof(index));

            var parsedResults = _queryOperationResults[index]?.GetParsedResults<TResult>();
            return parsedResults;
        }

        public GraphQLQueryResults<TResult> GetResults<TResult>(string operationName) where TResult : class
        {
            operationName.AssertArgIsNotNullOrBlank(nameof(operationName));
            
            var parsedResults = _queryOperationResultLookup[operationName]
                .FirstOrDefault()
                ?.GetParsedResults<TResult>();

            return parsedResults;
        }

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public GraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(int index) where TResult : class
            => GetResults<TResult>(index) as GraphQLQueryConnectionResult<TResult>;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public GraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class
            => GetResults<TResult>(operationName) as GraphQLQueryConnectionResult<TResult>;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public GraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class
        {
            var connectionResults = GetConnectionResults<TResult>(operationName);
            if (connectionResults == null)
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
