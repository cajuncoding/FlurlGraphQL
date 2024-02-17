using System;
using System.Collections.Generic;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    internal class FlurlGraphQLSystemTextJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public static IFlurlGraphQLResponseProcessor FromFlurlGraphQLResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public FlurlGraphQLSystemTextJsonResponseProcessor(object data, List<GraphQLError> errors, IFlurlGraphQLSystemTextJsonSerializer systemTextJsonSerializer)
        {
            this.Data = data;
            this.Errors = errors?.AsReadOnly();
            this.JsonSerializer = systemTextJsonSerializer.AssertArgIsNotNull(nameof(systemTextJsonSerializer));
        }

        #region Non-interface Properties
        public IFlurlGraphQLJsonSerializer JsonSerializer { get; }
        #endregion

        public object Data { get; }
        public IReadOnlyList<GraphQLError> Errors { get; }

        public IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public IGraphQLBatchQueryResults LoadBatchQueryResults()
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public string GetErrorContent()
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }
    }
}
