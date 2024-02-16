using System.Collections.Generic;

namespace FlurlGraphQL
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

        public string Message { get; }
        public IReadOnlyList<GraphQLErrorLocation> Locations { get; }
        public IReadOnlyList<object> Path { get; }
        public IReadOnlyDictionary<string, object> Extensions { get; }
    }

    public class GraphQLErrorLocation
    {
        public GraphQLErrorLocation(uint column, uint line)
        {
            Column = column;
            Line = line;
        }

        public uint Column { get; }
        public uint Line { get; }
    }
}
