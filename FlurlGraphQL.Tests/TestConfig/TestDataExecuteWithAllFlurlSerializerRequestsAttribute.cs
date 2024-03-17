using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.Tests.TestConfig;

namespace FlurlGraphQL.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class TestDataExecuteWithAllFlurlSerializerRequestsAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            var graphQLApiEndpoint = FlurlGraphQLTestConfiguration.GraphQLApiEndpoint;

            yield return new object[]
            {
                //FIRST return a System.Text.Json based Flurl Url for testing with Newtonsoft Serialization!
                new Url(graphQLApiEndpoint).WithSettings(settings =>
                {
                    settings.JsonSerializer = new DefaultJsonSerializer(); //<== ENFORCE Default (System.Text.Json) Serializer!
                })
            };

            yield return new object[]
            {
                //SECOND return a Newtonsoft.Json based Flurl Url for testing with Newtonsoft Serialization!
                new Url(graphQLApiEndpoint).WithSettings(settings =>
                {
                    settings.JsonSerializer = new NewtonsoftJsonSerializer(); //<== ENFORCE Newtonsoft.Json Serializer!
                })
            };
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] dataParamArray)
        {
            var flurlRequest = dataParamArray.FirstOrDefault() as IFlurlRequest;
            var testCaseDescription = flurlRequest?.Settings.JsonSerializer switch
            {
                var s when s is DefaultJsonSerializer || s is IFlurlGraphQLSystemTextJsonSerializer => "Flurl Test [System.Text.Json] Serializer",
                var s when s is NewtonsoftJsonSerializer || s is IFlurlGraphQLNewtonsoftJsonSerializer => "Flurl Test [Newtonsoft.Json] Serializer",
                _ => "Flurl Test [Unknown] Serializer"
            };

            return $"{methodInfo.Name} - {testCaseDescription}";
        }
    }
}
