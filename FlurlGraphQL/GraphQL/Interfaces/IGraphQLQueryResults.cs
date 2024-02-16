using System.Collections.Generic;

namespace FlurlGraphQL
{
    public interface IGraphQLQueryResults<out TResult> : IReadOnlyList<TResult>
        where TResult: class
    {
        bool HasAnyResults();
        IReadOnlyList<GraphQLError> Errors { get; }
        bool HasAnyErrors();
    }

    //Provide Convenience Method for accessing Internal methods of the interface implementation...
    public static class IGraphQLQueryResultsExtensions
    {
        internal static IGraphQLConnectionResults<TResult> ToGraphQLConnectionResultsInternal<TResult>(this IGraphQLQueryResults<TResult> results) where TResult : class
        {
            if (results == null)
                return null;

            return results is IGraphQLConnectionResults<TResult> connectionResults
                ? connectionResults
                //If all we have are results then we can't provide TotalCount so it's null (e.g. because it wasn't requested/available)...
                : new GraphQLConnectionResults<TResult>(results);
        }

        internal static IGraphQLCollectionSegmentResults<TResult> ToCollectionSegmentResultsInternal<TResult>(this IGraphQLConnectionResults<TResult> connectionResults)
            where TResult : class
        {
            if (connectionResults == null)
                return null;

            var pageInfo = connectionResults.PageInfo;

            //Always convert the Connection results (which can be re-used effectively) to Collection Segment results...
            return new GraphQLCollectionSegmentResults<TResult>(
                connectionResults,
                connectionResults.TotalCount,
                new GraphQLOffsetPageInfo(hasNextPage: pageInfo?.HasNextPage, hasPreviousPage: pageInfo?.HasPreviousPage)
            );
        }
    }

}