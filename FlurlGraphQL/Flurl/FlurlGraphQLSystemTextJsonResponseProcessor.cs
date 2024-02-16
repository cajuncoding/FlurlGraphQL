using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonResponseProcessor : IFlurlGraphQLResponseProcessor
    {
        public static IFlurlGraphQLResponseProcessor FromFlurlGraphQLResponse(IFlurlGraphQLResponse graphqlResponse)
        {
            //TODO: WIP...
            throw new NotImplementedException();
        }

        public FlurlGraphQLSystemTextJsonResponseProcessor(object data, List<GraphQLError> errors, IReadOnlyDictionary<string, object> contextBag, IFlurlGraphQLSystemTextJsonSerializer systemTextJsonSerializer)
        {
            this.Data = data;
            this.Errors = errors?.AsReadOnly();
            //We MUST to pass along the ContextBag (internal) which may contain configuration details for processing the payload results...
            this.ContextBag = contextBag;
            this.JsonSerializer = systemTextJsonSerializer.AssertArgIsNotNull(nameof(systemTextJsonSerializer));
        }

        #region Non-interface Properties
        public IFlurlGraphQLJsonSerializer JsonSerializer { get; }
        #endregion

        public object Data { get; }
        public IReadOnlyList<GraphQLError> Errors { get; }
        public IReadOnlyDictionary<string, object> ContextBag { get; }
        public IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null) where TResult : class
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
