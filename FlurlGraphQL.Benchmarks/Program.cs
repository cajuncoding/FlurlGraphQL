using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

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

            //*******************DEBUG***********************
            //var benchmark = new FlurlGraphQLParsingBenchmarks();
            //benchmark.GlobalSetup();
            ////benchmark.ParsingNewtonsoftJson();
            //benchmark.ParsingWithSystemTextJson();
        }
    }
}