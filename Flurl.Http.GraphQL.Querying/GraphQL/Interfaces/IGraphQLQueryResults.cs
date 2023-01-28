using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLQueryResults<out TResult> : IReadOnlyList<TResult>
        where TResult: class
    {
        bool HasAnyResults();
        IList<GraphQLError> Errors { get; }
        bool HasAnyErrors();
        int? TotalCount { get; }
    }

    //Provide Convenience Method for accessing Internal methods of the interface implementation...
    public static class IGraphQLQueryResultsExtensions
    {
        internal static IList<TResult> GetResultsInternal<TResult>(this IGraphQLQueryResults<TResult> results) where TResult : class
            => (results as GraphQLQueryResults<TResult>)?.GetResultsInternal() ?? new List<TResult>();
    }

}