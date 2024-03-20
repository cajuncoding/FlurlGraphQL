using System;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.TypeCacheHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.JsonProcessing
{
    public class FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter : JsonConverter
    {
        private Type _skipTypeToPreventInfiniteRecursion = null;

        public FlurlGraphQLNewtonsoftJsonPaginatedResultsConverter()
        {
        }

        public override bool CanConvert(Type objectType)
        {
            bool canConvert = objectType != _skipTypeToPreventInfiniteRecursion && IsTypeSupportedForConversion(objectType);

            //Clear our RecursionSkip if Not Converting this!
            if (!canConvert)
                _skipTypeToPreventInfiniteRecursion = null;

            return canConvert;
        }

        private bool IsTypeSupportedForConversion(Type objectType)
        {
            return objectType.InheritsFrom(GraphQLTypeCache.ICollection)
                   //All GraphQL paging wrapper types implement the base GraphQLQueryResultsType...
                   || objectType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer jsonSerializer) => throw new NotImplementedException();

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type targetType, object existingValue, JsonSerializer jsonSerializer)
        {
            object entityResults = null;

            //Since we must interrogate the Json it is significantly easier to deserialize into a dynamic JsonNode first...
            var json = JToken.ReadFrom(reader);

            //All Paginated result sets, that can be flattened, are a Json Object with nested nodes/items/edges properties that are
            //  the actual array of results we can collapse into. Therefore we only need to process Object nodes, otherwise
            //  we fallback to original Json handling below...
            if (json.Type == JTokenType.Object)
            {
                //All GraphQL wrapper types implement the base GraphQLQueryResultsType so we check it first to know if it's a complex or simplified Data Model...
                if (targetType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType))
                {
                    entityResults = json.ToObject(targetType, jsonSerializer);
                }
                else if (json.Field(GraphQLFields.Nodes) is JArray nodesJson)
                {
                    entityResults = nodesJson.ToObject(targetType, jsonSerializer);
                }
                else if (json.Field(GraphQLFields.Items) is JArray itemsJson)
                {
                    entityResults = itemsJson.ToObject(targetType, jsonSerializer);
                }
                else if (json.Field(GraphQLFields.Edges) is JArray edgesJson)
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .ToObject(targetType, jsonSerializer);
                }
            }

            //We fallback to original Json handling here if not already processed...
            if (entityResults == null)
            {
                //At this point the results have not been handled above therefore we attempt to fallback to default Newtonsoft Json behavior,
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
                _skipTypeToPreventInfiniteRecursion = targetType;
                entityResults = json.ToObject(targetType, jsonSerializer);
            }

            return entityResults;
        }
    }

}
