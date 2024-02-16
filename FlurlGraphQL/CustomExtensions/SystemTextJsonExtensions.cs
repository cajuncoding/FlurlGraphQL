using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlurlGraphQL.SystemTextJsonExtensions
{
    public static class SystemTextJsonExtensions
    {
        public static string ToJsonStringIndented(this JsonNode json, JsonSerializerOptions options = null)
        {
            if (json == null) return string.Empty;

            var serializerOptions = options ?? new JsonSerializerOptions();
            serializerOptions.WriteIndented = true;

            return json.ToJsonString(serializerOptions);
        }
    }
}
