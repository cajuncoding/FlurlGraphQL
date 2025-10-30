using System;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests
{
    [TestClass]
    public class FlurlGraphQLMutationTests : BaseFlurlGraphQLTest
    {

        [TestMethod]
        [TestDataExecuteWithAllFlurlSerializerRequests]
        public async Task TestMutationWithQueryResultsAsync(IFlurlRequest graphqlApiRequest)
        {
            var mutationResult = await graphqlApiRequest
                .WithGraphQLQuery(@"
                    mutation($reviewInput: CreateReviewInput) {
	                    createReview(input: $reviewInput) {
		                    episode
		                    review {
			                    id
			                    stars
			                    commentary
		                    }
	                    }
                    }
                ")
                .SetGraphQLVariable("reviewInput", new {
                    episode = "EMPIRE",
                    stars = 5,
                    StarsEnum = StarsEnum.FiveStars,
                    commentary = "I love this Movie!"
                })
                .PostGraphQLQueryAsync()
                .ReceiveGraphQLMutationResult<CreateReviewPayload>()
                .ConfigureAwait(false);

            Assert.IsNotNull(mutationResult);
            Assert.IsFalse(string.IsNullOrEmpty(mutationResult.Episode));
            Assert.IsNotNull(mutationResult.Review);
            Assert.IsFalse(string.IsNullOrEmpty(mutationResult.Review.Commentary));
            Assert.IsNotNull(mutationResult.Review.Id);
            Assert.AreNotEqual(Guid.Empty, mutationResult.Review.Id);

            var jsonText = JsonConvert.SerializeObject(mutationResult, Formatting.Indented);
            TestContext.WriteLine(jsonText);
        }
    }

    public enum StarsEnum
    {
        OneStar = 1,
        TwoStars = 2,
        ThreeStars = 3,
        FourStars = 4,
        FiveStars = 5
    };

    public class CreateReviewPayload {
        public string Episode { get; set; }
        public ReviewResult Review {get; set;}

        public class ReviewResult
        {
            public Guid Id { get; set; }
            public int Stars { get; set; }
            public string Commentary { get; set; }
        }
    }
}