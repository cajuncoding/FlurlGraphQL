using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flurl.Http.GraphQL.Querying.NewtonsoftJson
{
    internal class GraphQLPaginatedResultsJsonConverter : JsonConverter
    {
        protected HashSet<Type> TypesToSkipCache { get; set; } = new HashSet<Type>();

        public GraphQLPaginatedResultsJsonConverter()
        {
            Debug.Write($"Constructing new [{nameof(GraphQLPaginatedResultsJsonConverter)}]!!!");
        }
        
        public override bool CanConvert(Type objectType)
        {
            //return IsEnumerableType(objectType);
            //return typeof(IEnumerable).IsAssignableFrom(objectType);
            bool canConvert = typeof(IEnumerable).IsAssignableFrom(objectType) && !TypesToSkipCache.Contains(objectType);
            return canConvert;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer jsonSerializer) 
            => throw new NotImplementedException();

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            object results = null;

            switch (reader.TokenType)
            {
                case JsonToken.StartObject when !TypesToSkipCache.Contains(objectType):
                {
                    var json = JObject.Load(reader);

                    //if (json.Field(GraphQLFields.Nodes) != null || json.Field(GraphQLFields.Edges) != null || json.Field(GraphQLFields.Items) != null)
                    //{
                    //    var parseJsonMethodInfo = ResolveGenericParseJsonDelegate(objectType);
                    //    var results = parseJsonMethodInfo.Invoke(null, new object[]{ json });
                    //    return nodes.ToObject(objectType, jsonSerializer);
                    //}
                    if (json.Field(GraphQLFields.Nodes) is JArray nodes)
                    {
                        results = nodes.ToObject(objectType, jsonSerializer);
                    }
                    else
                    {
                        results = json.ToObject(objectType, jsonSerializer);
                    }

                    break;
                }
            }

            if (results == null)
            {
                TypesToSkipCache.Add(objectType);
                results = jsonSerializer.Deserialize(reader, objectType);
            }

            return results;
        }

        //protected virtual bool IsEnumerableType(Type objectType)
        //{
        //    //TODO: Optimize with type caching if this works as expected...
        //    if (typeof(IEnumerable).IsAssignableFrom(objectType))
        //        return true;
        //    else if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        //        return true;
        //    else
        //        return false;
        //}

        //protected virtual bool IsGraphQLPaginationType(Type objectType)
        //{
        //    //TODO: Optimize with type caching if this works as expected...
        //    if (typeof(IEnumerable).IsAssignableFrom(objectType))
        //    {
        //        return true;
        //    }
        //    else if (objectType.IsGenericType)
        //    {
        //        var genericTypeDef = objectType.GetGenericTypeDefinition();
        //        if (genericTypeDef == typeof(IGraphQLConnectionResults<>)
        //            || genericTypeDef == typeof(IGraphQLCollectionSegmentResults<>)
        //            || genericTypeDef == typeof(IGraphQLQueryResults<>)
        //           )
        //        {
        //            return true;
        //        }
        //    }
                
        //    return false;
        //}

    //    protected virtual MethodInfo ResolveGenericParseJsonDelegate(Type objectType)
    //    {
    //        //TODO: Optimize this for performance (compling, caching, etc.)...
    //        //Type graphQLResultsType = typeof(IGraphQLQueryResults<>).MakeGenericType(objectType);
    //        //Type jsonParseFuncType = typeof(Func<,>).MakeGenericType(typeof(JToken), graphQLResultsType);
            
    //        //var compiledDelegate = Delegate.CreateDelegate(jsonParseFuncType, ParseJsonToGraphQLResultsMethodInfo);
    //        //return compiledDelegate;
    //        return ParseJsonToGraphQLResultsMethodInfo.MakeGenericMethod(objectType);
    //    }

    //    private static readonly MethodInfo ParseJsonToGraphQLResultsMethodInfo = typeof(GraphQLPaginatedResultsJsonConverter)
    //        .GetMethods(BindingFlags.Static)
    //        .FirstOrDefault(pi => pi.Name.Equals(nameof(ParseJsonToGraphQLResultsInternal)));

    //    internal static IGraphQLQueryResults<TResult> ParseJsonToGraphQLResultsInternal<TResult>(JToken json)
    //        where TResult : class
    //    {
    //        if (json == null)
    //            return new GraphQLQueryResults<TResult>();

    //        var jsonSerializer = JsonSerializer.CreateDefault();
    //        jsonSerializer.ContractResolver = GraphQLAdaptiveJsonContractResolver.Instance;

    //        //Dynamically parse the data from the results...
    //        //NOTE: We process PageInfo as Cursor Paging as the Default (because it's strongly encouraged by GraphQL.org
    //        //          & Offset Paging model is a subset of Cursor Paging (less flexible).
    //        var pageInfo = json.Field(GraphQLFields.PageInfo)?.ToObject<GraphQLCursorPageInfo>();
    //        var totalCount = (int?)json.Field(GraphQLFields.TotalCount);

    //        PaginationType? paginationType = null;
    //        List<TResult> entityResults = null;

    //        //Dynamically resolve the Results from:
    //        // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
    //        // - the Items child of the Data Result (for items{} based Offset Paginated queries)
    //        // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
    //        // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
    //        if (json.Field(GraphQLFields.Nodes) is JArray nodes)
    //        {
    //            entityResults = nodes.ToObject<List<TResult>>(jsonSerializer);
    //            paginationType = PaginationType.Cursor;
    //        }
    //        else if (json.Field(GraphQLFields.Items) is JArray items)
    //        {
    //            entityResults = items.ToObject<List<TResult>>(jsonSerializer);
    //            paginationType = PaginationType.Offset;
    //        }
    //        //Handle Edges case (which allow access to the Cursor)
    //        else if (json.Field(GraphQLFields.Edges) is JArray edges)
    //        {
    //            paginationType = PaginationType.Cursor;
    //            var entityType = typeof(TResult);

    //            //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges!
    //            if (entityType.IsDerivedFromGenericParent(typeof(GraphQLEdge<>)))
    //            {
    //                //If the current type is a Generic GraphQLEdge<TEntity> then we can directly deserialize to the Generic Type!
    //                entityResults = edges.Select(edge => edge?.ToObject<TResult>(jsonSerializer)).ToList();
    //            }
    //            //Handle all other cases including when the Entity implements IGraphQLEdge (e.g. the entity has a Cursor Property)...
    //            else
    //            {
    //                entityResults = edges.OfType<JObject>().Select(edge =>
    //                {
    //                    var entityEdge = edge.Field(GraphQLFields.Node)?.ToObject<TResult>(jsonSerializer);

    //                    //If the entity implements IGraphQLEdge (e.g. the entity has a Cursor Property), then we can specify the Cursor...
    //                    if (entityEdge is IGraphQLEdge cursorEdge)
    //                        cursorEdge.Cursor = (string)edge.Field(GraphQLFields.Cursor);

    //                    return entityEdge;
    //                }).ToList();
    //            }
    //        }
    //        else switch (json)
    //        {
    //            case JArray arrayResults:
    //                entityResults = arrayResults.ToObject<List<TResult>>(jsonSerializer);
    //                break;
    //            case JObject jsonObj when jsonObj.First is JArray firstArrayResults:
    //                entityResults = firstArrayResults.ToObject<List<TResult>>(jsonSerializer);
    //                break;
    //        }

    //        //If the results have Paging Info we map to the correct type (Connection/Cursor or CollectionSegment/Offset)...
    //        //NOTE: If we have a Total Count then we also must return a Paging result because it's possible to
    //        //      request TotalCount by itself without any other PageInfo or Nodes...
    //        if (paginationType == PaginationType.Cursor || totalCount.HasValue)
    //        {
    //            return new GraphQLConnectionResults<TResult>(entityResults, totalCount, pageInfo);
    //        }
    //        else if (paginationType == PaginationType.Offset)
    //        {
    //            return new GraphQLCollectionSegmentResults<TResult>(entityResults, totalCount, GraphQLOffsetPageInfo.FromCursorPageInfo(pageInfo));
    //        }

    //        //If not a paging result then we simply return the typed results...
    //        return new GraphQLQueryResults<TResult>(entityResults);
    //    }
    }

}
