using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Querying.NewtonsoftJson
{
    public class GraphQLPageResultsToICollectionConverter : JsonConverter
    {

        private Type SkipTypeToPreventInfiniteRecursion = null;

        public GraphQLPageResultsToICollectionConverter()
        {
        }
        
        public override bool CanConvert(Type objectType)
        {
            bool canConvert = objectType != SkipTypeToPreventInfiniteRecursion && objectType.InheritsFrom(GraphQLTypeCache.ICollection);

            //Clear our RecursionSkip if Not Converting this!
            if(!canConvert)
                SkipTypeToPreventInfiniteRecursion = null;
            
            return canConvert;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer jsonSerializer) 
            => throw new NotImplementedException();

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            object results = null;

            var json = JToken.ReadFrom(reader);

            if (json.Type == JTokenType.Object)
            {
                if (json.Field(GraphQLFields.Nodes) is JArray nodes)
                {
                    results = nodes.ToObject(objectType, jsonSerializer);
                }
            }

            if (results == null)
            {
                SkipTypeToPreventInfiniteRecursion = objectType;
                results = json.ToObject(objectType, jsonSerializer);
            }

            return results;
        }
    }

}
