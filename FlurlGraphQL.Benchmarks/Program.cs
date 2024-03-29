using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FlurlGraphQL.Benchmarks.TestData;

namespace FlurlGraphQL.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            var summary = BenchmarkRunner.Run<FlurlGraphQLParsingBenchmarks>(config, args);

            // Use this to select benchmarks from the console:
            // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

            //*******************GENERATE TEST DATA JSON FILE (with Fake/Bogus Data) ***********************
            //var testDataGenerator = new BooksAndAuthorsTestDataGenerator();
            //testDataGenerator.GenerateAndWriteToTestDataJsonFile();
            //return;

            //*******************DEBUG * **********************
            //var benchmark = new FlurlGraphQLParsingBenchmarks();
            //benchmark.GlobalSetup();
            //benchmark.ParsingWithNewtonsoftJsonConverter();
            //benchmark.ParsingWithNewtonsoftJsonRewriting();
            //benchmark.ParsingWithSystemTextJsonRewriting();
        }
    }
}