using System;
using System.Collections;
using System.Collections.Generic;

namespace FlurlGraphQL
{
    /// <summary>
    /// Contains the direct typed results of a single GraphQL query.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class GraphQLQueryResults<TResult> : IReadOnlyList<TResult>, IGraphQLQueryResults<TResult> 
        where TResult : class
    {
        public GraphQLQueryResults(IReadOnlyList<TResult> results = null, IReadOnlyList<GraphQLError> errors = null)
        {
            //The Results should be null safe as an empty list if no results exist.
            Results = results ?? new List<TResult>().AsReadOnly();
            
            //Errors may be null however as they are an exception case.
            Errors = errors;
        }

        public IReadOnlyList<GraphQLError> Errors { get; protected internal set; }

        public bool HasAnyResults() => Results.Count > 0;

        public bool HasAnyErrors() => Errors?.Count > 0;

        #region IReadOnlyList / IEnumerable Implementation
        protected IReadOnlyList<TResult> Results { get; }

        public IEnumerator<TResult> GetEnumerator() => Results.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => Results.Count;
        public TResult this[int index] => Results[index];
        
        #endregion
    }
}
