using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying
{
    public class GraphQLError
    {
        public GraphQLError(string message = null, List<GraphQLErrorLocation> locations = null, List<object> path = null, IReadOnlyDictionary<string, object> extensions = null)
        {
            Message = message;
            Locations = locations?.AsReadOnly();
            Path = path?.AsReadOnly();
            Extensions = extensions;
        }

        [JsonProperty("message")]
        public string Message { get; }

        [JsonProperty("locations")]
        public IReadOnlyList<GraphQLErrorLocation> Locations { get; }

        [JsonProperty("path")]
        public IReadOnlyList<object> Path { get; }

        [JsonProperty("extensions")]
        public IReadOnlyDictionary<string, object> Extensions { get; }
    }

    public class GraphQLErrorLocation
    {
        public GraphQLErrorLocation(uint column, uint line)
        {
            Column = column;
            Line = line;
        }

        [JsonProperty("column")]
        public uint Column { get; }
        [JsonProperty("line")]
        public uint Line { get; }
    }
}
