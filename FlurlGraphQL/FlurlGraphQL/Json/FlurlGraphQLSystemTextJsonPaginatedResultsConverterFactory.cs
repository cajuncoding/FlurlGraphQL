using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.SystemTextJsonExtensions;
using FlurlGraphQL.TypeCacheHelpers;

namespace FlurlGraphQL
{
    public class FlurlGraphQLSystemTextJsonPaginatedResultsConverterFactory : JsonConverterFactory
    {
        protected static ConcurrentDictionary<Type, Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>> ConverterCache { get; }
            = new ConcurrentDictionary<Type, Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>>();

        public FlurlGraphQLSystemTextJsonPaginatedResultsConverterFactory()
        {
        }

        //TODO: If there are no Infinite Loop issues like with Newtonsoft.Json then we can simplify this without the additional method...
        public override bool CanConvert(Type objectType) => IsTypeSupportedForConversion(objectType);

        private bool IsTypeSupportedForConversion(Type objectType)
        {
            //NOTE: We check for ICollection as it is the most widely implemented interface across all primary
            //          collection types in .NET (Array, List, Dictionary, etc.)...
            return objectType.InheritsFrom(GraphQLTypeCache.ICollection)
                   //All FlurlGraphQL paging wrapper types implement the IGraphQLQueryResultsType (open generics) interface...
                   || objectType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
        }

        public override JsonConverter CreateConverter(Type targetType, JsonSerializerOptions options)
        {
            var converterLazy = ConverterCache.GetOrAdd(targetType, new Lazy<IFlurlGraphQLSystemTextJsonPaginatedResultsConverter>(() =>
            {
                JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                    typeof(FlurlGraphQLSystemTextJsonPaginatedResultsConverter<>).MakeGenericType(targetType),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: null,
                    culture: null
                );

                return (IFlurlGraphQLSystemTextJsonPaginatedResultsConverter)converter;
            }));

            return converterLazy.Value.ToJsonConverter();
        }
    }

    public interface IFlurlGraphQLSystemTextJsonPaginatedResultsConverter
    {
        JsonConverter ToJsonConverter();
    }

    public class FlurlGraphQLSystemTextJsonPaginatedResultsConverter<TCollection> : JsonConverter<TCollection>, IFlurlGraphQLSystemTextJsonPaginatedResultsConverter
    {
        public override TCollection Read(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions jsonSerializerOptions)
        {
            //TODO: Determine if we should Throw Exception or just return when non-object is detected...
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            object entityResults = null;

            //Since we must interrogate the Json it is significantly easier to deserialize into a dynamic JsonNode first...
            var json = JsonSerializer.Deserialize<JsonNode>(ref reader, jsonSerializerOptions);

            //All Paginated result sets, that can be flattened, are a Json Object with nested nodes/items/edges properties that are
            //  the actual array of results we can collapse into. Therefore we only need to process Object nodes, otherwise
            //  we fallback to original Json handling below...
            if (json.GetValueKind() == JsonValueKind.Object)
            {
                //All GraphQL wrapper types implement the base GraphQLQueryResultsType so we check it first to know if it's a complex or simplified Data Model...
                if (targetType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType))
                {
                    entityResults = JsonSerializer.Deserialize(ref reader, targetType, jsonSerializerOptions);
                }
                else if (json?[GraphQLFields.Nodes] is JsonArray nodesJson)
                {
                    entityResults = nodesJson.Deserialize(targetType, jsonSerializerOptions);
                }
                else if (json?[GraphQLFields.Items] is JsonArray itemsJson)
                {
                    entityResults = itemsJson.Deserialize(targetType, jsonSerializerOptions);
                }
                else if (json?[GraphQLFields.Edges] is JsonArray edgesJson)
                {
                    entityResults = edgesJson
                        .FlattenGraphQLEdgesJsonToArrayOfNodes()
                        .Deserialize(targetType, jsonSerializerOptions); ;
                }
            }

            //We fallback to original Json handling here if not already processed...
            if (entityResults == null)
                entityResults = json.Deserialize(targetType, jsonSerializerOptions);

            return (TCollection)entityResults;
        }

        public override void Write(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options) => throw new NotImplementedException();
        
        public JsonConverter ToJsonConverter() => (JsonConverter)this;
    }

}
