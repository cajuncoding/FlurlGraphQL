using System.Collections;
using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Querying
{
    /// <summary>
    /// Contains the direct typed results of a single GraphQL query.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class GraphQLQueryResults<TResult> : IReadOnlyList<TResult>, IGraphQLQueryResults<TResult> 
        where TResult : class
    {
        public GraphQLQueryResults(IList<TResult> results = null, IList < GraphQLError> errors = null)
        {
            //The Results should be null safe as an empty list if no results exist.
            Results = results ?? new List<TResult>();
            
            //Errors may be null however as they are an exception case.
            Errors = errors;
        }

        protected IList<TResult> Results { get; }
        public bool HasAnyResults() => Results.Count > 0;
        // Internal Helper for Constructing results (e.g. GraphQLCollectionSegmentResults paging which is adapted from the Connection Results)...
        internal IList<TResult> GetResultsInternal() => Results;

        public IList<GraphQLError> Errors { get; }
        public bool HasAnyErrors() => this.Errors?.Count > 0;

        #region IReadOnlyList / IEnumerable Implementation

        public IEnumerator<TResult> GetEnumerator() => Results.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => Results.Count;
        public TResult this[int index] => Results[index];
        
        #endregion
    }
}
