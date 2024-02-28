//using System;
//using System.Collections.Concurrent;
//using System.Reflection;
//using System.Text.Json;
//using System.Text.Json.Nodes;
//using System.Text.Json.Serialization;
//using FlurlGraphQL.ReflectionExtensions;
//using FlurlGraphQL.SystemTextJsonExtensions;
//using FlurlGraphQL.TypeCacheHelpers;

//namespace FlurlGraphQL
//{
//    public class FlurlGraphQLSystemTextJsonPaginatedResultsConverterFactory : JsonConverterFactory
//    {
//        protected static ConcurrentDictionary<Type, Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>> ConverterCache { get; }
//            = new ConcurrentDictionary<Type, Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>>();

//        public override bool CanConvert(Type targetType)
//        {
//            if (ConverterCache.TryGetValue(targetType, out var lazy))
//            {
//                var converter = lazy.Value;
//                if (converter != null)
//                {
//                    if (targetType != converter.SkipTypeToPreventInfiniteRecursion && GraphQLJsonConverterHelper.IsTypeSupportedForConversion(targetType))
//                        return true;

//                    converter.SkipTypeToPreventInfiniteRecursion = null;
//                    return false;
//                }
//            }

//            return GraphQLJsonConverterHelper.IsTypeSupportedForConversion(targetType); ;
//        }

//        public override JsonConverter CreateConverter(Type targetType, JsonSerializerOptions options)
//        {
//            var converterLazy = ConverterCache.GetOrAdd(targetType, new Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>(() =>
//            {
//                var jsonConverter = (JsonConverter)Activator.CreateInstance(
//                    typeof(FlurlGraphQLSystemTextJsonPaginatedResultsConverter<>).MakeGenericType(targetType),
//                    BindingFlags.Instance | BindingFlags.Public,
//                    binder: null,
//                    args: null,
//                    culture: null
//                );

//                return (IFlurlGraphQLSystemTextJsonPaginatedResultsConverter)jsonConverter;
//            }));

//            return converterLazy.Value.ToJsonConverter();
//        }
//    }

//    public interface IFlurlGraphQLSystemTextJsonPaginatedResultsConverter
//    {
//        JsonConverter ToJsonConverter();
//        Type SkipTypeToPreventInfiniteRecursion { get; set; }
//    }

//    public class FlurlGraphQLSystemTextJsonPaginatedResultsConverter<TCollection> : JsonConverter<TCollection>, IFlurlGraphQLSystemTextJsonPaginatedResultsConverter
//    {
//        public Type SkipTypeToPreventInfiniteRecursion { get; set; } = null;

//        public override bool CanConvert(Type targetType)
//        {
//            if (targetType != SkipTypeToPreventInfiniteRecursion && GraphQLJsonConverterHelper.IsTypeSupportedForConversion(targetType))
//                return true;

//            SkipTypeToPreventInfiniteRecursion = null;
//            return false;
//        }

//        public override TCollection Read(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions jsonSerializerOptions)
//        {
//            //TODO: Cleanup if this is should not be included...
//            //if (reader.TokenType != JsonTokenType.StartArray)
//            //    throw new JsonException("Current Json reader is not pointing at JsonArray data; only implementations of ICollection with JsonArray data are supported.");

//            object entityResults = null;

//            //Process an Object...

//            //var c = jsonSerializerOptions.GetConverter(typeof(IFlurlGraphQLSystemTextJsonPaginatedResultsConverter)) as IFlurlGraphQLSystemTextJsonPaginatedResultsConverter;
//            //var i = c.NewJsonConverterInstance();
//            //jsonSerializerOptions.Converters.Remove(c as JsonConverter);

//            //if (reader.TokenType == JsonTokenType.StartObject && reader.Read())
//            //{
//            //    if(reader.TokenType == JsonTokenType.PropertyName && (reader.GetString()?.Equals(GraphQLFields.Edges, StringComparison.OrdinalIgnoreCase) ?? false))


//            //    var depth = reader.CurrentDepth;
//            //    while (reader.TokenType != JsonTokenType.PropertyName && reader.Read())
//            //    {
//            //    }


//            //}
//            //else
//            //{

//            //}



//            //Since we must interrogate the Json it is significantly easier to deserialize into a dynamic JsonNode first...
//            var json = JsonSerializer.Deserialize<JsonNode>(ref reader, jsonSerializerOptions);

//            //All Paginated result sets, that can be flattened, are a Json Object with nested nodes/items/edges properties that are
//            //  the actual array of results we can collapse into. Therefore we only need to process Object nodes, otherwise
//            //  we fallback to original Json handling below...
//            if (json.GetValueKind() == JsonValueKind.Object)
//            {
//                //All GraphQL wrapper types implement the base GraphQLQueryResultsType so we check it first to know if it's a complex or simplified Data Model...
//                if (targetType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType))
//                {
//                    entityResults = json.Deserialize(targetType, jsonSerializerOptions);
//                }
//                else if (json?[GraphQLFields.Nodes] is JsonArray nodesJson)
//                {
//                    entityResults = nodesJson.Deserialize(targetType, jsonSerializerOptions);
//                }
//                else if (json?[GraphQLFields.Items] is JsonArray itemsJson)
//                {
//                    entityResults = itemsJson.Deserialize(targetType, jsonSerializerOptions);
//                }
//                else if (json?[GraphQLFields.Edges] is JsonArray edgesJson)
//                {
//                    entityResults = edgesJson
//                        .FlattenGraphQLEdgesToJsonArray()
//                        .Deserialize(targetType, jsonSerializerOptions); ;
//                }
//            }

//            //We fallback to original Json handling here if not already processed...
//            if (entityResults == null)
//            {
//                //At this point the results have not been handled above therefore we attempt to fallback to default Newtonsoft Json behavior,
//                //      however due to the design of JsonConverters this results in an Infinite Recursive loop. So we must
//                //      track our state and flag the current objectType as the specific Type to skip when it's next encountered
//                //      which should always be the next call to CanConvert() which will then return false, and reset this Skip flag to Null!
//                //This process successfully interrupts the recursive loop and allows default processing to take place by NewtonsoftJson, and by
//                //      resetting it we enable support for the type to be re-used multiple times because we only skip this next instance
//                //NOTE: This was the only algorithm that works as expected because CanConvert() only receives a Type and nothing else, and
//                //      all other state monitoring properties of JsonReader such as reader.Path either don't change, get reset due to new
//                //      JsonTokeReader instantiations (inside Newtonsoft) and/or are simply private and not-accessible, etc.
//                //NOTE: This algorithm is based on the assumption that the JsonTokenReaders are always reading in one direction (synchronously)
//                //      and that this Converter is only accessed by one Serializer at a time (Not Thread-safe) so the Converter should never be
//                //      added to the Global or Default settings of a Serializer... ONLY added just prior to specific de-serialization executions!
//                SkipTypeToPreventInfiniteRecursion = targetType;
//                entityResults = json.Deserialize(targetType, new JsonSerializerOptions(jsonSerializerOptions));
//                //entityResults = JsonSerializer.Deserialize<TCollection>(ref reader, jsonSerializerOptions);
//            }

//            return (TCollection)entityResults;
//        }

//        public override void Write(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options)
//            => throw new NotImplementedException();

//        public JsonConverter ToJsonConverter()
//            => (JsonConverter)this;
//    }

//    internal static class GraphQLJsonConverterHelper
//    {
//        internal static bool IsTypeSupportedForConversion(Type objectType)
//        {
//            //NOTE: We check for ICollection as it is the most widely implemented interface across all primary
//            //          collection types in .NET (Array, List, Dictionary, etc.)...
//            return objectType.InheritsFrom(GraphQLTypeCache.ICollection)
//                   //All FlurlGraphQL paging wrapper types implement the IGraphQLQueryResultsType (open generics) interface...
//                   || objectType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
//        }
//    }
//}
