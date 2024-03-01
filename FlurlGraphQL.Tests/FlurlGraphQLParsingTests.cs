using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using FlurlGraphQL.FlurlGraphQL.Json;
using FlurlGraphQL.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLParsingTests : BaseFlurlGraphQLTest
    {
        #region Sample Json Strings

        private string NestedJsonStructureFlattenedWithForceEnum { get; } = @"
            [
                {
                    ""cursor"": ""MQ=="",
                    ""friends"": [
                        {
                            ""cursor"": ""MQ=="",
                            ""height"": 1.72,
                            ""name"": ""C-3PO"",
                            ""personalIdentifier"": 2000,
                            ""theForce"": ""NONE""
                        },
                        {
                            ""cursor"": ""Mg=="",
                            ""height"": 1.72,
                            ""name"": ""Han Solo"",
                            ""personalIdentifier"": 1002,
                            ""theForce"": ""NONE""
                        }
                    ],
                    ""height"": 1.72,
                    ""name"": ""Luke Skywalker"",
                    ""personalIdentifier"": 1000,
                    ""theForce"": ""LIGHT_SIDE""
                },
                {
                    ""cursor"": ""Mg=="",
                    ""friends"": [
                        {
                            ""cursor"": ""MQ=="",
                            ""height"": 1.72,
                            ""name"": ""Wilhuff Tarkin"",
                            ""personalIdentifier"": 1004,
                            ""theForce"": ""NONE""
                        }
                    ],
                    ""height"": 1.72,
                    ""name"": ""Darth Vader"",
                    ""personalIdentifier"": 1001,
                    ""theForce"": ""DARK_SIDE""
                }
            ]
        ";

        private string NestedPaginatedJsonText { get; } = @"
            {
	            ""data"": {
		            ""characters"": {
			            ""totalCount"": 8,
			            ""pageInfo"": {
				            ""hasNextPage"": true,
				            ""hasPreviousPage"": false,
				            ""startCursor"": ""MQ=="",
				            ""endCursor"": ""Mg==""
			            },
			            ""edges"": [
				            {
					            ""cursor"": ""MQ=="",
					            ""node"": {
						            ""personalIdentifier"": 1000,
						            ""name"": ""Luke Skywalker"",
						            ""height"": 1.72,
						            ""friends"": {
							            ""edges"": [
								            {
									            ""cursor"": ""MQ=="",
									            ""node"": {
										            ""personalIdentifier"": 2000,
										            ""name"": ""C-3PO"",
										            ""height"": 1.72
									            }
								            },
								            {
									            ""cursor"": ""Mg=="",
									            ""node"": {
										            ""personalIdentifier"": 1002,
										            ""name"": ""Han Solo"",
										            ""height"": 1.72
									            }
								            }
							            ]
						            }
					            }
				            },
				            {
					            ""cursor"": ""Mg=="",
					            ""node"": {
						            ""personalIdentifier"": 1001,
						            ""name"": ""Darth Vader"",
						            ""height"": 1.72,
						            ""friends"": {
							            ""edges"": [
								            {
									            ""cursor"": ""MQ=="",
									            ""node"": {
										            ""personalIdentifier"": 1004,
										            ""name"": ""Wilhuff Tarkin"",
										            ""height"": 1.72
									            }
								            }
							            ]
						            }
					            }
				            }
			            ]
		            }
	            }
            }
        ";

        #endregion

        [TestMethod]
        public void TestSimpleSystemTextJsonParsingOfPreFlattenedJsonWithStringEnumWithAnnotationJsonConverter()
        {
            var jsonOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            jsonOptions.Converters.Add(new JsonStringEnumMemberConverter(allowIntegerValues: true));
            //jsonOptions.Converters.Add(new JsonStringEnumConverter());

            var characterResults = JsonSerializer.Deserialize<List<StarWarsCharacterWithEnum>>(NestedJsonStructureFlattenedWithForceEnum, jsonOptions);

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
        }

        [TestMethod]
        public void TestSystemTextJsonParsingOfNestedPaginatedGraphQLResults()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedJsonText);

            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacter>().ToGraphQLConnectionResultsInternal();

            AssertCursorPaginatedResultsAreValid(characterResults);
        }

        [TestMethod]
        public void TestSystemTextJsonParsingOfNestedPaginatedGraphQLResultsWithJsonPropertyNameMappings()
        {
            var systemTextJsonGraphQLProcessor = CreateDefaultSystemTextJsonGraphQLResponseProcessor(this.NestedPaginatedJsonText);

            //NOTE: This will explicitly test/exercise (also for Debugging) the Internal Extension method and by extension also
            //			the custom Json Converter [FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter]!
            var characterResults = systemTextJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacterWithJsonMappings>().ToGraphQLConnectionResultsInternal();

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

        [TestMethod]
        public void TestNewtonsoftJsonParsingOfNestedPaginatedGraphQLResults()
        {
            var newtonsoftJsonGraphQLProcessor = CreateDefaultNewtonsoftJsonGraphQLResponseProcessor(this.NestedPaginatedJsonText);

            var characterResults = newtonsoftJsonGraphQLProcessor.LoadTypedResults<StarWarsCharacter>().ToGraphQLConnectionResultsInternal();

            AssertCursorPaginatedResultsAreValid(characterResults);
        }

        private void AssertCursorPaginatedResultsAreValid(IGraphQLConnectionResults<StarWarsCharacter> characterResults)
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