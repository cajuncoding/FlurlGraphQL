using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.Querying.Tests
{
    [TestClass]
    public class FlurlGraphQLMutationTests : BaseFlurlGraphQLTest
    {
 
        [TestMethod]
        public async Task TestMutationWithQueryResultsAsync()
        {
            var inputPayload = JArray.Parse(@"
                [{
                        ""eventId"": 23,
                        ""eventUUID"": ""c5cd1cca-fe4d-490d-a948-985023c6185c"",
                        ""name"": ""RÜFÜS DU SOL"",
                        ""eventType"": ""ONE_OFF"",
                        ""status"": ""APPROVED"",
                        ""eventDate"": ""2018-11-01T07:00:00"",
                        ""announceDate"": null,
                        ""onSaleDate"": null,
                        ""doorTime"": null,
                        ""newElvisVenueId"": 10811,
                        ""internalBudget"": 15000.00000000,
                        ""externalBudget"": 15000.00000000,
                        ""budgetCurrencyCode"": ""USD"",
                        ""budgetExchangeRate"": 1.000000,
                        ""bookerDescription"": null,
                        ""companyMasterId"": 1,
                        ""subledger"": ""H6367996"",
                        ""genreDescription"": ""Dance / Electronic / DJ / Techno / House / Trance"",
                        ""notes"": null,
                        ""headlinerArtists"": [{
                                ""artistMasterId"": 0,
                                ""artistOrdinalSortId"": 1
                            }
                        ],
                        ""supportingArtists"": [{
                                ""artistMasterId"": 0,
                                ""artistOrdinalSortId"": 1
                            }
                        ],
                        ""eventContacts"": null,
                        ""shows"": [{
                                ""showId"": 101,
                                ""showName"": ""Show #3"",
                                ""showOrdinalSortId"": 3,
                                ""showDate"": ""2018-11-03T07:00:00"",
                                ""announceDate"": null,
                                ""onSaleDate"": null,
                                ""doorTime"": null
                            }, {
                                ""showId"": 102,
                                ""showName"": ""Show #2"",
                                ""showOrdinalSortId"": 2,
                                ""showDate"": ""2018-11-02T07:00:00"",
                                ""announceDate"": null,
                                ""onSaleDate"": null,
                                ""doorTime"": null
                            }, {
                                ""showId"": 103,
                                ""showName"": ""Show #1"",
                                ""showOrdinalSortId"": 1,
                                ""showDate"": ""2018-11-01T07:00:00"",
                                ""announceDate"": null,
                                ""onSaleDate"": null,
                                ""doorTime"": null
                            }
                        ],
                        ""createdDate"": ""2018-11-02T21:13:34"",
                        ""lastUpdatedDate"": ""2020-06-11T10:00:12""
                    }]
                ");

            var mutationResult = await "http://localhost:7072/api/v1/graphql?code=Tsk4dixKihdUyRlvlcDu1dHETHYIiCPpLawk%2F27apOsRTr1LbhF7vw%3D%3D"
                .WithGraphQLQuery(@"
                    mutation ($eventInputArray: [EventCreateOrUpdateInput]) {
	                    eventsCreateOrUpdate(input: $eventInputArray) {
		                    eventResults {
			                    eventUUID
			                    eventId
		                    }
		                    errors {
			                    ... on Error {
				                    errorCode
				                    message
			                    }
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariables(new { eventInputArray = inputPayload })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLMutationResult<EventsCreateOrUpdateResult>()
                .ConfigureAwait(false);


            Assert.IsNotNull(mutationResult);
            Assert.IsTrue(mutationResult.EventResults.Length > 0);

            var jsonText = JsonConvert.SerializeObject(mutationResult, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }

    public class EventsCreateOrUpdateResult
    {
        public EventResult[] EventResults { get; set; }
    }

    public class EventResult
    {
        public Guid? EventUUID { get; set; }
        public int? EventId { get; set; }
    }
}