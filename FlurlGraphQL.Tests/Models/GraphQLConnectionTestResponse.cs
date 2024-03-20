using System.Collections.Generic;

namespace FlurlGraphQL.Tests.Models
{
    internal class GraphQLConnectionTestResponse<TEntity>
    {
        public GraphQLConnectionTestResponse(string operationName, GraphQLConnectionTestResults<TEntity> operationResults)
        {
            Data = new Dictionary<string, GraphQLConnectionTestResults<TEntity>>()
            {
                [operationName] = operationResults
            };
        }

        public Dictionary<string, GraphQLConnectionTestResults<TEntity>> Data { get; }
    }
}
