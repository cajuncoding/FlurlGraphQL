using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLNewtonsoftJsonResponseRewriteProcessor : FlurlGraphQLNewtonsoftJsonResponseBaseProcessor, IFlurlGraphQLResponseProcessor
    {
        public FlurlGraphQLNewtonsoftJsonResponseRewriteProcessor(JObject rawDataJObject, List<GraphQLError> errors, FlurlGraphQLNewtonsoftJsonSerializer newtonsoftJsonSerializer)
            : base(rawDataJObject, errors, newtonsoftJsonSerializer)
        {
        }

        public override IGraphQLQueryResults<TResult> LoadTypedResults<TResult>(string queryOperationName = null)
        {
            var rawDataJson = (JObject)this.RawDataJObject;

            //BBernard
            //Extract the data results for the operation name specified, or first results as default (most common use case)...
            //NOTE: GraphQL supports multiple data responses per request so we need to access the correct query type result safely (via Null Coalesce)
            var querySingleResultJson = string.IsNullOrWhiteSpace(queryOperationName)
                ? rawDataJson.FirstField()
                : rawDataJson.Field(queryOperationName);

            var typedResults = querySingleResultJson.ConvertNewtonsoftJsonToGraphQLResultsWithJsonSerializerInternal<TResult>(JsonSerializer.JsonSerializerSettings);

            //Ensure that the Results we return are initialized along with any potential Errors (that have already been parsed/captured)... 
            if (this.Errors != null && typedResults is GraphQLQueryResults<TResult> graphqlResults)
                graphqlResults.Errors = this.Errors;

            return typedResults;
        }
    }
}
