using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using System.IO;
using System.Linq;
using FlurlGraphQL.Benchmarks.Models;

namespace FlurlGraphQL.Benchmarks
{
    public class FlurlGraphQLParsingBenchmarks
    {
        protected string JsonSource { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            System.Diagnostics.Debugger.Launch();
            JsonSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), @"TestData\LargeGraphQLTestDataSet.json"));
        }

        [Benchmark(Baseline = true)]
        public void ParsingNewtonsoftJson()
        {
            var newtonsoftJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.JsonSource);

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<BenchmarkModel>().ToGraphQLConnectionResultsInternal();
        }

        [Benchmark]
        public void ParsingWithSystemTextJson()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.JsonSource);

            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<BenchmarkModel>().ToGraphQLConnectionResultsInternal();
        }

        #region Test Helpers

        private IFlurlGraphQLResponseProcessor CreateDefaultSystemTextJsonGraphQLResponseProcessor(string jsonText)
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(new DefaultJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<SystemTextJsonGraphQLResult>(jsonText);

            return new FlurlGraphQLSystemTextJsonResponseProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLSystemTextJsonSerializer
            );
        }

        private IFlurlGraphQLResponseProcessor CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(string jsonText)
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(jsonText);

            return new FlurlGraphQLNewtonsoftJsonResponseProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLNewtonsoftJsonSerializer
            );
        }

        #endregion

    }
}
