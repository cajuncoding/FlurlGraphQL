using BenchmarkDotNet.Attributes;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.Benchmarks.TestData;
using FlurlGraphQL.Tests.Models;

namespace FlurlGraphQL.Benchmarks
{
    public class FlurlGraphQLParsingBenchmarks
    {
        protected string JsonSource { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            System.Diagnostics.Debugger.Launch();

            var testDataGenerator = new BooksAndAuthorsTestDataGenerator();
            JsonSource = testDataGenerator.GenerateJsonSource();

            //JsonSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), @"TestData\BooksAndAuthorsCursorPaginatedLargeDataSet.json"));
        }

        [Benchmark(Baseline = true)]
        public void ParsingWithNewtonsoftJsonConverter()
        {

            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(this.JsonSource);

            var newtonsoftJsonGraphQLProcessor = new FlurlGraphQLNewtonsoftJsonResponseConverterProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLNewtonsoftJsonSerializer
            );

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();
        }

        [Benchmark]
        public void ParsingWithNewtonsoftJsonRewriting()
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(this.JsonSource);

            var newtonsoftJsonGraphQLProcessor = new FlurlGraphQLNewtonsoftJsonResponseRewriteProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLNewtonsoftJsonSerializer
            );

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();
        }

        [Benchmark]
        public void ParsingWithSystemTextJsonRewriting()
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(new DefaultJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<SystemTextJsonGraphQLResult>(this.JsonSource);

            var systemTextJsonGraphQLProcessor = new FlurlGraphQLSystemTextJsonResponseProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLSystemTextJsonSerializer
            );

            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();
        }
    }
}
