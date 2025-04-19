using System.Text.Json;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLSystemTextJsonScreamingCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToScreamingCase();
    }
}
