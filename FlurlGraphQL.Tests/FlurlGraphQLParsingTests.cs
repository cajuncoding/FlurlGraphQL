using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.CustomExtensions;
using FlurlGraphQL.JsonProcessing;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Bogus.DataSets;
using System.Runtime.Serialization;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLParsingTests : BaseFlurlGraphQLTest
    {
        public FlurlGraphQLParsingTests() : base()
        {
            NestedJsonStructureFlattenedWithForceEnum = LoadTestData("NestedPreFlattened.StarWarsDataWithForceEnum.json");
        }

        #region Sample Json Strings

        private string NestedJsonStructureFlattenedWithForceEnum { get; }

        #endregion

        #region Newtonsoft Json Parsing tests (using Default System.Text.Json Transform Processor)...

        [TestMethod]
        public void TestDefaultSystemTextJsonProcessorIsCorrect()
        {
            var newtonsoftJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);
            Assert.IsInstanceOfType(newtonsoftJsonGraphQLProcessor, typeof(FlurlGraphQLSystemTextJsonResponseTransformProcessor));
        }

        [TestMethod]
        public void TestSimpleSystemTextJsonParsingOfErrors()
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(new DefaultJsonSerializer());

            var errorJsonText = LoadTestData("Errors.SimpleTestData.json");
            var graphqlErrors = graphqlSerializer.ParseErrorsFromGraphQLExceptionErrorContent(errorJsonText);

            AssertSimpleErrorTestDataIsValid(graphqlErrors);
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public void TestSimpleSystemTextJsonParsingOfPreFlattenedJsonWithStringEnumWithEnumMemberAnnotationConversion(IFlurlRequest flurlRequest)
        {
            var graphqlApiRequest = flurlRequest.ToGraphQLRequest();
            //Be sure that we ignore Null properties so that the serialization Comparison at the end works as expected!
            switch (graphqlApiRequest.GraphQLJsonSerializer)
            {
                case FlurlGraphQLSystemTextJsonSerializer _:
                    graphqlApiRequest.UseGraphQLSystemTextJson(o =>
                    {
                        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    });
                    break;
                case FlurlGraphQLNewtonsoftJsonSerializer _:
                    graphqlApiRequest.UseGraphQLNewtonsoftJson(s =>
                    {
                        s.NullValueHandling = NullValueHandling.Ignore;
                    });
                    break;
            }

            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = graphqlApiRequest.GraphQLJsonSerializer;

            var characterResults = graphqlSerializer.Deserialize<List<StarWarsCharacterWithEnumMember>>(NestedJsonStructureFlattenedWithForceEnum);

            Assert.IsNotNull(characterResults);
            Assert.AreEqual(2, characterResults.Count);
            Assert.AreEqual(TheForceWithEnumMembers.LightSideForGood, characterResults.First(c => c.Name.StartsWith("Luke")).TheForce);
            Assert.AreEqual(TheForceWithEnumMembers.DarkSideForEvil, characterResults.First(c => c.Name.StartsWith("Darth")).TheForce);

            foreach (var result in characterResults)
            {
                TestContext.WriteLine($"Character [{result.PersonalIdentifier}] [{result.Name}] [{result.Height}]");

                foreach (var friend in result.Friends)
                {
                    Assert.IsNotNull(friend);
                    Assert.AreEqual(TheForceWithEnumMembers.NoneAtAll, friend.TheForce);
                    TestContext.WriteLine($"   Friend [{friend.PersonalIdentifier}] [{friend.Name}] [{friend.Height}]");
                }
            }

            //TEST Re-serialization should Match the original Json!
            var json = graphqlSerializer.Serialize(characterResults);
            bool isEqual = JToken.DeepEquals(
                JArray.Parse(NestedJsonStructureFlattenedWithForceEnum), //The Test Data
                JArray.Parse(json) //The Round Trip Serialized data
            );

            Assert.IsTrue(isEqual, "The Test Json and the Round Trip Serialized Json do not match!");
        }


        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public void TestSimpleSystemTextJsonParsingOfPreFlattenedJsonWithStringEnumDirectConversion(IFlurlRequest flurlRequest)
        {
            var graphqlApiRequest = flurlRequest.ToGraphQLRequest();
            //Be sure that we ignore Null properties so that the serialization Comparison at the end works as expected!
            switch (graphqlApiRequest.GraphQLJsonSerializer)
            {
                case FlurlGraphQLSystemTextJsonSerializer _:
                    graphqlApiRequest.UseGraphQLSystemTextJson(o =>
                    {
                        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    });
                    break;
                case FlurlGraphQLNewtonsoftJsonSerializer _:
                    graphqlApiRequest.UseGraphQLNewtonsoftJson(s =>
                    {
                        s.NullValueHandling = NullValueHandling.Ignore;
                    });
                    break;
            }

            var graphqlSerializer = graphqlApiRequest.GraphQLJsonSerializer;

            var characterResults = graphqlSerializer.Deserialize<List<StarWarsCharacterWithSimpleEnum>>(NestedJsonStructureFlattenedWithForceEnum);

            Assert.IsNotNull(characterResults);
            Assert.AreEqual(2, characterResults.Count);
            Assert.AreEqual(TheForce.LightSide, characterResults.First(c => c.Name.StartsWith("Luke")).TheForce);
            Assert.AreEqual(TheForce.DarkSide, characterResults.First(c => c.Name.StartsWith("Darth")).TheForce);

            foreach (var result in characterResults)
            {
                TestContext.WriteLine($"Character [{result.PersonalIdentifier}] [{result.Name}] [{result.Height}]");

                foreach (var friend in result.Friends)
                {
                    Assert.IsNotNull(friend);
                    Assert.AreEqual(TheForce.None, friend.TheForce);
                    TestContext.WriteLine($"   Friend [{friend.PersonalIdentifier}] [{friend.Name}] [{friend.Height}]");
                }
            }

            //TEST Re-serialization should Match the original Json!
            var json = graphqlSerializer.Serialize(characterResults);
            bool isEqual = JToken.DeepEquals(
                JArray.Parse(NestedJsonStructureFlattenedWithForceEnum), //The Test Data
                JArray.Parse(json) //The Round Trip Serialized data
            );

            Assert.IsTrue(isEqual, "The Test Json and the Round Trip Serialized Json do not match!");
        }

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public void TestJsonParsingForAllStringEnumTestCases(IFlurlRequest flurlRequest)
        {
            //The DEFAULT options for FlurlGraphQL System.Text.Json Serializer should already have the String Enum Converter added...
            //var flurlGraphQLSystemTextJsonSerializer = new FlurlGraphQLSystemTextJsonSerializer(FlurlGraphQLSystemTextJsonSerializer.CreateDefaultSerializerOptions());
            var graphqlApiRequest = flurlRequest.ToGraphQLRequest();

            var graphqlJsonSerializer = graphqlApiRequest.GraphQLJsonSerializer;

            TestContext.WriteLine($"[{graphqlJsonSerializer.GetType().Name}] Enum Test Cases Parsing ...");
            bool isUsingSystemTextJson = graphqlJsonSerializer is FlurlGraphQLSystemTextJsonSerializer;

            foreach (int enumIntValue in Enum.GetValues(typeof(EnumTestCase)))
            {
                var enumValue = (EnumTestCase)enumIntValue;
                var enumField = enumValue.GetType().GetField(enumValue.ToString());
                
                var expectedEnumStringValue =
                    enumField.GetCustomAttribute<EnumMemberAttribute>(true)?.Value
                    //JsonPropertyName is ONLY supported by System.Text.Json; Newtonsoft did not support this attribute for Enums...
                    ?? (isUsingSystemTextJson ? enumField.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name : null)
                    ?? enumValue.ToString().ToScreamingSnakeCase();

                //Serialize the enum test case...
                var serializedEnumValue = graphqlJsonSerializer.Serialize(enumValue);

                //The serialized value should be a quoted string output from Json Serialization...
                Assert.AreEqual($"\"{expectedEnumStringValue}\"", serializedEnumValue, $"The Enum Value [{enumValue}] did not serialize to the expected string value [{expectedEnumStringValue}]!");
                
                //Deserialize back to the Enum Value...
                var deserializedEnumValue = graphqlJsonSerializer.Deserialize<EnumTestCase>(serializedEnumValue);
                Assert.AreEqual(enumValue, deserializedEnumValue, $"The Enum Value [{enumValue}] did not deserialize back correctly from the serialized value [{serializedEnumValue}]!");

                //Valdate actual values for the OVERRIDE cases using Attributes...
                switch (enumValue)
                {
                    case EnumTestCase.TestPacalCaseEnumMember:
                        Assert.AreEqual("TEST_PASCAL_CASE_ENUM_MEMBER_OVERRIDE", expectedEnumStringValue);
                        break;
                    case EnumTestCase.TestJsonPropertyNameSupportedBySystemTextJson when isUsingSystemTextJson:
                        Assert.AreEqual("TEST.JsonPropertyName.Supported.By.System.Text.Json", expectedEnumStringValue);
                        break;
                }

                TestContext.WriteLine($"Success: [{enumValue}] <==> [{serializedEnumValue}]");
            }
        }

        [TestMethod]
        public void TestSystemTextJsonParsingOfNestedPaginatedStarWarsGraphQLResults()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);

            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacter>().ToGraphQLConnectionResultsInternal();

            AssertStarWarsCursorPaginatedResultsAreValid(characterResults);
        }

        [TestMethod]
        public void TestSystemTextJsonParsingOfNestedPaginatedBooksAndAuthorsGraphQLResults()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedBooksAndAuthorsJsonText);

            var bookResults = systemTextJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();
            
            AssertBooksAndAuthorsCursorPaginatedResultsAreValid(bookResults);
        }

        [TestMethod]
        public void TestSystemTextJsonParsingOfNestedPaginatedGraphQLResultsWithJsonPropertyNameMappings()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);

            //NOTE: This will explicitly test/exercise (also for Debugging) the Internal Extension method and by extension also
            //			the custom Json Converter [FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter]!
            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacterWithSystemTextJsonMappings>().ToGraphQLConnectionResultsInternal();

            Assert.AreEqual(2, characterResults.Count);
            foreach (var result in characterResults)
            {
                Assert.IsNotNull(result);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.MyCursor));
                TestContext.WriteLine($"Character [{result.MyPersonalIdentifier}] [{result.MyName}] [{result.MyHeight}]");

                foreach (var friend in result.MyFriends)
                {
                    Assert.IsNotNull(friend);
                    Assert.IsTrue(friend.MyPersonalIdentifier > 0);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.MyName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.MyCursor));
                    TestContext.WriteLine($"   Friend [{friend.MyPersonalIdentifier}] [{friend.MyName}] [{friend.MyHeight}]");
                }
            }
        }
        
        #endregion
        
        #region Newtonsoft Json Parsing tests (using Default Newtonsoft Json Transform Processor)...

        [TestMethod]
        public void TestDefaultNewtonsoftJsonProcessorIsCorrect()
        {
            var newtonsoftJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);
            Assert.IsInstanceOfType(newtonsoftJsonGraphQLProcessor, typeof(FlurlGraphQLNewtonsoftJsonResponseTransformProcessor));
        }


        [TestMethod]
        public void TestSimpleNewtonsoftJsonParsingOfErrors()
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());

            var errorJsonText = LoadTestData("Errors.SimpleTestData.json");
            var graphqlErrors = graphqlSerializer.ParseErrorsFromGraphQLExceptionErrorContent(errorJsonText);

            AssertSimpleErrorTestDataIsValid(graphqlErrors);
        }

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedStarWarsGraphQLResults()
        {
            var newtonsoftJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacter>().ToGraphQLConnectionResultsInternal();

            AssertStarWarsCursorPaginatedResultsAreValid(characterResults);
        }

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedBooksAndAuthorsGraphQLResults()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedBooksAndAuthorsJsonText);

            var bookResults = systemTextJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();

            AssertBooksAndAuthorsCursorPaginatedResultsAreValid(bookResults);
        }

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedGraphQLResultsWithJsonPropertyNameMappings()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);

            //NOTE: This will explicitly test/exercise (also for Debugging) the Internal Extension method and by extension also
            //			the custom Json Converter [FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter]!
            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacterWithNewtonsoftJsonMappings>().ToGraphQLConnectionResultsInternal();

            Assert.AreEqual(2, characterResults.Count);
            foreach (var result in characterResults)
            {
                Assert.IsNotNull(result);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.MyCursor));
                TestContext.WriteLine($"Character [{result.MyPersonalIdentifier}] [{result.MyName}] [{result.MyHeight}]");

                foreach (var friend in result.MyFriends)
                {
                    Assert.IsNotNull(friend);
                    Assert.IsTrue(friend.MyPersonalIdentifier > 0);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.MyName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.MyCursor));
                    TestContext.WriteLine($"   Friend [{friend.MyPersonalIdentifier}] [{friend.MyName}] [{friend.MyHeight}]");
                }
            }
        }
        
        #endregion

        #region Legacy Newtonsoft Converter Processor Tests (Keep for Posterity)...

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedBooksAndAuthorsGraphQLResultsLegacyConverterProcessor()
        {
            //var newtonsoftJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedStarWarsJsonText);
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(this.NestedPaginatedBooksAndAuthorsJsonText);

            var newtonsoftJsonGraphQLProcessor = new FlurlGraphQLNewtonsoftJsonResponseConverterProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLNewtonsoftJsonSerializer
            );

            var bookResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<Book>().ToGraphQLConnectionResultsInternal();

            AssertBooksAndAuthorsCursorPaginatedResultsAreValid(bookResults);
        }

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedGraphQLResultsUsingLegacyConverterProcessor()
        {
            var jsonText = this.NestedPaginatedStarWarsJsonText;

            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(jsonText);

            var newtonsoftJsonGraphQLProcessor = new FlurlGraphQLNewtonsoftJsonResponseConverterProcessor(
                graphqlResult.Data,
                graphqlResult.Errors,
                graphqlSerializer as FlurlGraphQLNewtonsoftJsonSerializer
            );

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacter>().ToGraphQLConnectionResultsInternal();

            AssertStarWarsCursorPaginatedResultsAreValid(characterResults);
        }

        #endregion

        #region Assert Validation Helperse

        private void AssertSimpleErrorTestDataIsValid(IReadOnlyList<GraphQLError> graphqlErrors)
        {
            Assert.IsNotNull(graphqlErrors);
            Assert.IsNotNull(graphqlErrors);
            Assert.AreEqual(1, graphqlErrors.Count);

            var error = graphqlErrors[0];
            Assert.IsNotNull(error);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error.Message));
            Assert.AreEqual(1, error.Locations.Count);
            Assert.AreEqual(1, error.Extensions.Count);

            var location = error.Locations[0];
            Assert.AreEqual((uint)2, location.Line);
            Assert.AreEqual((uint)32, location.Column);

            var extension = error.Extensions.First();
            Assert.IsNotNull(extension);
            Assert.AreEqual("code", extension.Key);
            Assert.AreEqual("HC0011", extension.Value);
        }

        private void AssertStarWarsCursorPaginatedResultsAreValid(IGraphQLConnectionResults<StarWarsCharacter> characterResults)
        {
            Assert.IsNotNull(characterResults);
            Assert.IsTrue(characterResults is IGraphQLConnectionResults<StarWarsCharacter>);
            Assert.AreEqual(2, characterResults.Count);

            Assert.IsNotNull(characterResults.TotalCount);
            Assert.IsTrue(characterResults.TotalCount > characterResults.Count);
            Assert.IsNotNull(characterResults.PageInfo);
            Assert.IsTrue(characterResults.PageInfo.HasNextPage);
            Assert.IsFalse(characterResults.PageInfo.HasPreviousPage);
            Assert.IsFalse(string.IsNullOrWhiteSpace(characterResults.PageInfo.StartCursor));
            Assert.IsFalse(string.IsNullOrWhiteSpace(characterResults.PageInfo.EndCursor));

            foreach (var result in characterResults)
            {
                Assert.IsNotNull(result);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Cursor));
                TestContext.WriteLine($"Character [{result.PersonalIdentifier}] [{result.Name}] [{result.Height}]");

                foreach (var friend in result.Friends)
                {
                    Assert.IsNotNull(friend);
                    Assert.IsTrue(friend.PersonalIdentifier > 0);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.Name));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(friend.Cursor));
                    TestContext.WriteLine($"   Friend [{friend.PersonalIdentifier}] [{friend.Name}] [{friend.Height}]");
                }
            }
        }

        private void AssertBooksAndAuthorsCursorPaginatedResultsAreValid(IGraphQLConnectionResults<Book> bookResults)
        {
            //TODO: Build out more thorough validation of these nested recursive results!
            Assert.IsNotNull(bookResults);
            Assert.AreEqual(2, bookResults.Count);

            var firstBook = bookResults[0];
            Assert.IsNotNull(firstBook);
            Assert.IsFalse(firstBook?.Name?.IsNullOrEmpty());

            var firstAuthor = firstBook.Authors.FirstOrDefault();
            Assert.IsNotNull(firstAuthor);
            Assert.IsFalse(firstAuthor?.FirstName?.IsNullOrEmpty());
            Assert.AreEqual(8, firstAuthor.AuthoredBooks.Length);
            Assert.AreEqual(3, firstAuthor.EditedBooks.Length);
        }
        
        #endregion

        #region Test Helpers

        private IFlurlGraphQLResponseProcessor CreateDefaultSystemTextJsonGraphQLResponseProcessor(string jsonText)
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = (FlurlGraphQLSystemTextJsonSerializer)FlurlGraphQLSystemTextJsonSerializer.FromFlurlSerializer(new DefaultJsonSerializer());
            //NOTE: This uses the same underlying de-serializer as the Flurl engine as it's a wrapped Flurl Serializer...
            var graphqlResult = graphqlSerializer.Deserialize<SystemTextJsonGraphQLResult>(jsonText);

            return graphqlSerializer.CreateGraphQLResponseProcessor(graphqlResult);
        }

        private IFlurlGraphQLResponseProcessor CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(string jsonText)
        {
            //NOTE: We leverage Internal Methods and Classes here to get lower level access for Unit Testing and Quicker Debugging...
            var graphqlSerializer = (FlurlGraphQLNewtonsoftJsonSerializer)FlurlGraphQLNewtonsoftJsonSerializer.FromFlurlSerializer(new NewtonsoftJsonSerializer());
            //NOTE: This uses the same underlying de-serializer as the Flurl engine as it's a wrapped Flurl Serializer...
            var graphqlResult = graphqlSerializer.Deserialize<NewtonsoftGraphQLResult>(jsonText);

            return graphqlSerializer.CreateGraphQLResponseProcessor(graphqlResult);
        }

        #endregion
    }
}