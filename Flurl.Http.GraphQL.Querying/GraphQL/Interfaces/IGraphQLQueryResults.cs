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
}