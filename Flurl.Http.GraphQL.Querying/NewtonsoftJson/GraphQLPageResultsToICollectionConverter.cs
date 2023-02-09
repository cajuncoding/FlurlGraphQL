using System;
using System.Collections.Generic;
using System.Linq;
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
            object entityResults = null;

            var json = JToken.ReadFrom(reader);

            //All Paginated result sets, that can be flattened, are a Json Object with nested nodes/items/edges properties that are
            //  the actual array of results we can collapse into. Therefore we only need to process Object nodes, otherwise
            //  we fallback to original Json handling below...
            if (json.Type == JTokenType.Object)
            {
                if (json.Field(GraphQLFields.Nodes) is JArray nodesJson)
                {
                    entityResults = nodesJson.ToObject(objectType, jsonSerializer);
                }
                else if (json.Field(GraphQLFields.Items) is JArray itemsJson)
                {
                    entityResults = itemsJson.ToObject(objectType, jsonSerializer);
                }
                else if (json.Field(GraphQLFields.Edges) is JArray edgesJson)
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .ToObject(objectType, jsonSerializer);
                }
            }

            //We fallback to original Json handling here if not already processed...
            if (entityResults == null)
            {
                //At this point the results have not been handled above therefore we attempt to fallback to default Newtonsoft Json behaviour,
                //      however due to the design of JsonConverters this results in an Infinite Recursive loop. So we must
                //      track our state and flag the current objectType as the specific Type to skip when it's next encountered
                //      which should always be the next call to CanConvert() which will then return false, and reset this Skip flag to Null!
                //This process successfully interrupts the recursive loop and allows default processing to take place by NewtonsoftJson, and by
                //      resetting it we enable support for the type to be re-used multiple times because we only skip this next instance
                //NOTE: This was the only algorithm that works as expected because CanConvert() only receives a Type and nothing else, and
                //      all other state monitoring properties of JsonReader such as reader.Path either don't change, get reset due to new
                //      JsonTokeReader instantiations (inside Newtonsoft) and/or are simply private and not-accessible, etc.
                //NOTE: This algorithm is based on the assumption that the JsonTokenReaders are always reading in one direction (synchronously)
                //      and that this Converter is only accessed by one Serializer at a time (Not Thread-safe) so the Converter should never be
                //      added to the Global or Default settings of a Serializer... ONLY added just prior to specific de-serialization executions!
                SkipTypeToPreventInfiniteRecursion = objectType;
                entityResults = json.ToObject(objectType, jsonSerializer);
            }

            return entityResults;
        }
    }

}
