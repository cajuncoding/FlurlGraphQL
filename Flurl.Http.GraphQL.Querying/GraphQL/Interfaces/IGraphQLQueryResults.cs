using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLQueryResults<out TResult> : IReadOnlyList<TResult>
        where TResult: class
    {
        bool HasAnyResults();
        IList<GraphQLError> Errors { get; }
        bool HasAnyErrors();
    }

    //Provide Convenience Method for accessing Internal methods of the interface implementation...
    public static class IGraphQLQueryResultsExtensions
    {
        internal static IList<TResult> GetResultsInternal<TResult>(this IGraphQLQueryResults<TResult> results) where TResult : class
            => (results as GraphQLQueryResults<TResult>)?.GetResultsInternal() ?? new List<TResult>();

        internal static IGraphQLQueryConnectionResult<TResult> ToGraphQLConnectionResultsInternal<TResult>(this IGraphQLQueryResults<TResult> results) where TResult : class
        {
            if (results == null)
                return null;

            return results is IGraphQLQueryConnectionResult<TResult> connectionResults
                ? connectionResults
                : new GraphQLQueryConnectionResult<TResult>(results.GetResultsInternal());
        }

        internal static IGraphQLQueryCollectionSegmentResult<TResult> ToCollectionSegmentResultsInternal<TResult>(this IGraphQLQueryConnectionResult<TResult> connectionResults)
            where TResult : class
        {
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