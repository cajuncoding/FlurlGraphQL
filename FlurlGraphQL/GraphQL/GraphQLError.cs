using FlurlGraphQL.SystemTextJsonExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FlurlGraphQL
{
    public class GraphQLError
    {
        public GraphQLError(string message = null, IReadOnlyList<GraphQLErrorLocation> locations = null, IReadOnlyList<object> path = null, IReadOnlyDictionary<string, object> extensions = null)
        {
            Message = message;
            Locations = locations;//?.AsReadOnly();
            Path = path;//?.AsReadOnly();
            Extensions = (IReadOnlyDictionary<string, object>)extensions?.Select(kv =>
            {
                //System.Text.Json does not infer C# types when de-serialzing into 'object' target types
                //  so we need to validate and re-map them here when necessary...
                return kv.Value is JsonElement jsonElement
                    ? new KeyValuePair<string, object>(kv.Key, jsonElement.ConvertToCSharpInferredType())
                    : kv;
            }).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public string Message { get; } = null;
        public IReadOnlyList<GraphQLErrorLocation> Locations { get; } = null;
        public IReadOnlyList<object> Path { get; } = null;
        public IReadOnlyDictionary<string, object> Extensions { get; } = null;
    }

    public class GraphQLErrorLocation
    {
        public GraphQLErrorLocation(uint line, uint column)
        {
            Line = line;
            Column = column;
        }

        public uint Column { get; }
        public uint Line { get; }
    }
}
