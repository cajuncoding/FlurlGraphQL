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

            var parsedResult = ResultJson.ConvertToGraphQLResultsInternal<TResult>();
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
            _queryOperationResultLookup = _queryOperationResults.ToLookup(r => r.OperationName, r => r);
        }

        public IGraphQLQueryResults<TResult> GetResults<TResult>(int index) where TResult : class
        {
            if(index < 0 || index > (_queryOperationResults.Count - 1))
                throw new ArgumentOutOfRangeException(nameof(index));

            var parsedResults = _queryOperationResults[index]?.GetParsedResults<TResult>();
            return parsedResults;
        }

        public IGraphQLQueryResults<TResult> GetResults<TResult>(string operationName) where TResult : class
        {
            operationName.AssertArgIsNotNullOrBlank(nameof(operationName));
            
            var parsedResults = _queryOperationResultLookup[operationName]
                .FirstOrDefault()
                ?.GetParsedResults<TResult>();

            return parsedResults;
        }

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(int index) where TResult : class
            => GetResults<TResult>(index) as IGraphQLQueryConnectionResult<TResult>;

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Cursor Pagination on a GraphQL Connection Operation compatible with the formalized Relay specification for Cursor Paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLQueryConnectionResult<TResult> GetConnectionResults<TResult>(string operationName) where TResult : class
            => GetResults<TResult>(operationName) as IGraphQLQueryConnectionResult<TResult>;


        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(int index) where TResult : class
        {
            if (GetConnectionResults<TResult>(index) is GraphQLQueryConnectionResult<TResult> connectionResults)
                return ConvertToCollectionSegmentResultsInternal(connectionResults);

            return null;
        }

        /// <summary>
        /// Gets the results along with any Pagination Details and/or Total Count that may have been optionally included in the Query.
        /// This assumes that the query used Offset Pagination on a GraphQL CollectionSegment Operation compatible with the HotChocolate GraphQL specification for offset paging.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public IGraphQLQueryCollectionSegmentResult<TResult> GetCollectionSegmentResults<TResult>(string operationName) where TResult : class
        {
            if (GetConnectionResults<TResult>(operationName) is GraphQLQueryConnectionResult<TResult> connectionResults)
                return ConvertToCollectionSegmentResultsInternal(connectionResults);
                
            return null;
        }

        private IGraphQLQueryCollectionSegmentResult<TResult> ConvertToCollectionSegmentResultsInternal<TResult>(GraphQLQueryConnectionResult<TResult> connectionResults) 
            where TResult : class
        {
            if (connectionResults == null) return null;

            var pageInfo = connectionResults.PageInfo;

            return new GraphQLQueryCollectionSegmentResult<TResult>(
                connectionResults.GetResultsInternal(),
                connectionResults.TotalCount,
                new GraphQLOffsetPageInfo(hasNextPage: pageInfo?.HasNextPage, hasPreviousPage: pageInfo?.HasPreviousPage)
            );
        }
    }
}
