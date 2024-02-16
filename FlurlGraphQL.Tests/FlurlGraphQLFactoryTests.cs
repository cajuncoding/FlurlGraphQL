using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLFactoryTests : BaseFlurlGraphQLTest
    {
        [TestMethod]
        public void TestFlurlGraphQLJsonSerializerFactoryPerformance()
        {

            int maxRuns = 1000;
            var timer = new Stopwatch();
            List<long> timeEntries = new List<long>();

            var flurlSystemTextJsonSerializer = new DefaultJsonSerializer();
            for (int i = 0; i < maxRuns; i++)
            {
                timer.Restart();
                var graphQLSerializer = FlurlGraphQLJsonSerializerFactory.FromFlurlSerializer(flurlSystemTextJsonSerializer);
                timer.Stop();
                
                if(i == 0) TestContext.WriteLine($"[SystemTextJson Test] First Execution Time was [{timer.Elapsed.TotalMilliseconds}] ms / [{timer.Elapsed.Ticks}] ticks...");
                timeEntries.Add(timer.ElapsedTicks);
            }

            var averageTime = TimeSpan.FromTicks((long)Math.Round(timeEntries.Average()));
            TestContext.WriteLine($"[SystemTextJson Test] Average Execution Time was [{averageTime.TotalMilliseconds}] ms / [{averageTime.Ticks}] ticks...");
            TestContext.WriteLine("");

            timeEntries.Clear();
            var flurlNewtonsoftJsonSerializer = new NewtonsoftJsonSerializer();
            for (int i = 0; i < maxRuns; i++)
            {
                timer.Restart();
                var graphQLSerializer = FlurlGraphQLJsonSerializerFactory.FromFlurlSerializer(flurlNewtonsoftJsonSerializer);
                timer.Stop();

                if (i == 0) TestContext.WriteLine($"[NewtonsoftJson Test] First Execution Time was [{timer.Elapsed.TotalMilliseconds}] ms / [{timer.Elapsed.Ticks}] ticks...");
                timeEntries.Add(timer.ElapsedTicks);
            }

            averageTime = TimeSpan.FromTicks((long)Math.Round(timeEntries.Average()));
            TestContext.WriteLine($"[NewtonsoftJson Test] Average Execution Time was [{averageTime.TotalMilliseconds}] ms / [{averageTime.Ticks}] ticks...");
            TestContext.WriteLine("");
        }
    }
}