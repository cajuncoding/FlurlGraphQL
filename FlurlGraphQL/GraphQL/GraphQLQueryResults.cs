using System.Collections;

namespace FlurlGraphQL
{
    /// <summary>
    /// Contains the direct typed results of a single GraphQL query.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class GraphQLQueryResults<TResult> : IReadOnlyList<TResult>, IGraphQLQueryResults<TResult> 
        where TResult : class
    {
        public GraphQLQueryResults(IList<TResult> results = null, IReadOnlyList<GraphQLError> errors = null)
        {
            //The Results should be null safe as an empty list if no results exist.
            Results = results ?? new List<TResult>();
            
            //Errors may be null however as they are an exception case.
            Errors = errors;
        }

        //NOTE: Due to various code flows and inheritance it's not easy (or clean) to pass the Errors through
        //      to the constructor so to simplify we have support to Internally Set this value when building the Results...
        //      This is NOT part of the public interface and is flagged as Obsolete to ensure it's not used externally!
        /// <summary>
        /// This method is for internal use only and may change at any time!
        /// </summary>
        /// <param name="errors"></param>
        [Obsolete("This method is for internal use only and may change at any time!")]
        public void SetErrorsInternal(IReadOnlyList<GraphQLError> errors)
        {
            Errors = errors;
        }

        protected IList<TResult> Results { get; }
        public bool HasAnyResults() => Results.Count > 0;
        // Internal Helper for Constructing results (e.g. GraphQLCollectionSegmentResults paging which is adapted from the Connection Results)...
        internal IList<TResult> GetResultsInternal() => Results;

        public IReadOnlyList<GraphQLError> Errors { get; protected set; }
        public bool HasAnyErrors() => this.Errors?.Count > 0;

        #region IReadOnlyList / IEnumerable Implementation

        public IEnumerator<TResult> GetEnumerator() => Results.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => Results.Count;
        public TResult this[int index] => Results[index];
        
        #endregion
    }
}
