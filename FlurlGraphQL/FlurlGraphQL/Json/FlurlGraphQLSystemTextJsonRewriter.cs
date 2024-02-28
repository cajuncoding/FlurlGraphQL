using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlurlGraphQL.CustomExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL.FlurlGraphQL.Json
{
    public class FlurlGraphQLSystemTextJsonRewriter
    {
        #region Factory Methods
        
        public static FlurlGraphQLSystemTextJsonRewriter ForType<T>() 
            => new FlurlGraphQLSystemTextJsonRewriter(FlurlGraphQLJsonRewriterTypeInfo.ForType<T>());

        #endregion

        public FlurlGraphQLJsonRewriterTypeInfo JsonRewriterTypeInfo { get; }
        public FlurlGraphQLSystemTextJsonRewriter(FlurlGraphQLJsonRewriterTypeInfo jsonRewriterTypeInfo)
        {
            JsonRewriterTypeInfo = jsonRewriterTypeInfo.AssertArgIsNotNull(nameof(jsonRewriterTypeInfo));
        }
        
        /// <summary>
        /// Rewrites the GraphQL Json results to flatten/simplify the hierarchy and support simplified/easier model mapping so that domain
        ///     models do not need to be polluted with GraphQL specific things like edges, nodes, items, etc.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public (JsonNode Json, PaginationType? PaginationType) RewriteJsonAsNeededForEasyGraphQLModelMapping(JsonNode json)
        {
            if (json == null) return (null, null);

            //NOTE: We really only need to know the Pagination Type of the Parent/Root node so that we can correctly determine the proper
            //          type of Paginated results (e.g. ConnectionResults vs CollectionSegmentResults) for the FlurlGraphQL root results type
            //          which nearly always is a simple List<> of Models, so we wrap them in our Results at the top level.
            //      All others will be defined as core types of the base model implementing IFlurlGraphQLQueryResults or any one of the derived
            //          default classes which can be directly de-serialized to.
            var paginationType = DeterminePaginationType(json);

            var rewrittenJson = RewriteGraphQLJsonAsNeededRecursively(json);

            return (rewrittenJson, paginationType);
        }

        /// <summary>
        /// Rewrite the Json as needed (and only where needed) to simplify the mapping of the Json to the provided Entity Model!
        ///     This will flatten complex GraphQL hierarchies such as [edges], [nodes], [items], etc. into simplified models as needed.
        /// The specified Entity Model defines what nodes are re-written as we only mutate the nodes necessary to attempt to match to the simplified model
        ///     and don't do any further processing other than what is defined in the JsonRewriterTypeInfo which is built from the Entity Model Class structure!
        ///
        /// NOTE: When rewriting our Json we ONLY need to handle the Model properties that should be Flattened; meaning they implement ICollection
        ///          but do not implement our convenience/wrapper classes that derive from IGraphQLQueryResults (which can be directly de-serialized without rewriting).
        ///      Therefore this dramatically reduces the number and types of Json fields we need to look at (and conceptually maps to the original FlurlGraphQL v1.x logic
        ///          using the Newtonsoft JsonConverter approach which only ran on ICollection property types).
        /// </summary>
        /// <param name="json"></param>
        /// <param name="recursiveRewriterPropInfos"></param>
        /// <returns></returns>
        protected JsonNode RewriteGraphQLJsonAsNeededRecursively(JsonNode json, IList<FlurlGraphQLJsonRewriterPropInfo> recursiveRewriterPropInfos = null)
        {
            //If not currently processing recursively then we default to the Root Properties of the JsonRewriterTypeInfo...
            var propsToRewrite = recursiveRewriterPropInfos ?? this.JsonRewriterTypeInfo.ChildPropertiesToRewrite;

            //NOTE: For Performance we ONLY process IF we actually have any properties to process otherwise we short circuit as there is no work to do!!!
            if (propsToRewrite.HasAny())
            {
                if (json is JsonObject jsonObject)
                {
                    foreach (var rewriterPropInfo in propsToRewrite)
                    {
                        var jsonPropNode = jsonObject[rewriterPropInfo.PropertyMappedJsonName];

                        //Ensure this is Null safe and Fast by skipping anytime the Json is not a valid Object containing a [nodes], [items], or [edges] property...
                        if (jsonPropNode is JsonObject jsonPropObject)
                        {
                            //First rewrite the current Node... 
                            RewriteGraphQLJsonObjectAsNeeded(jsonPropObject, rewriterPropInfo);
                            //Then Recursively Rewrite all child Json Properties as needed...
                            RewriteGraphQLJsonAsNeededRecursively(jsonObject, rewriterPropInfo.ChildPropertiesToRewrite);
                        }
                        else if (jsonPropNode is JsonArray jsonPropArray)
                        {
                            //If the value is an Array then we need to again recursively process all items in the Array as needed...
                            foreach (var jsonItem in jsonPropArray)
                                RewriteGraphQLJsonAsNeededRecursively(jsonItem, propsToRewrite);
                        }
                    }
                }
                else if (json is JsonArray jsonArray)
                {
                    //Iterate and recursively process all items in the Array as needed...
                    foreach (var jsonItem in jsonArray)
                        RewriteGraphQLJsonAsNeededRecursively(jsonItem, propsToRewrite);
                }
            }

            //Finally return our re-written/mutated json!
            return json;
        }

        protected JsonArray RewriteGraphQLJsonObjectAsNeeded(JsonObject json, FlurlGraphQLJsonRewriterPropInfo rewriterPropInfo)
        {
            var entityNodes = Enumerable.Empty<JsonObject>();

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            //NOTE: When relocating the nodes we must Clear the original parent Node to explicitly remove all children so that the Nodes can be re-assigned
            //          to a new location in the Json; as System.Text.Json will throw exceptions otherwise!
            if (json[GraphQLFields.Nodes] is JsonArray nodesJson)
            {
                entityNodes = nodesJson.OfType<JsonObject>();
                nodesJson.Clear();
            }
            else if (json[GraphQLFields.Items] is JsonArray itemsJson)
            {
                entityNodes = itemsJson.OfType<JsonObject>();
                itemsJson.Clear();
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json[GraphQLFields.Edges] is JsonArray edgesJson)
            {
                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges (maintaining the more complex GraphQL model).
                //  Otherwise we simplify/flatten the edge hierarchy into an Array of Objects for simplified model mapping/deserialization!
                entityNodes = rewriterPropInfo.ImplementsIGraphQLEdge 
                    ? edgesJson.OfType<JsonObject>() 
                    : FlattenGraphQLEdgesToJsonArray(edgesJson);

                edgesJson.Clear();
            }

            var rewrittenJsonArray = new JsonArray(entityNodes.OfType<JsonNode>().ToArray());
            return rewrittenJsonArray;
        }

        protected IEnumerable<JsonObject> FlattenGraphQLEdgesToJsonArray(JsonArray edgesArray)
        {
            return edgesArray
                .OfType<JsonObject>()
                .Select(edge =>
                {
                    //NOW we must MOVE / Re-locate all Nodes into our output JsonArray which means we have to remove them from the Parent
                    //  to avoid "Node already has a Parent" exceptions...
                    var node = edge[GraphQLFields.Node] as JsonObject;

                    //If not already defined, we map the Edges Cursor value to the Node so that the model is simplified
                    //  and any consumer can just add a "Cursor" property to their model to get the node's cursor.
                    if (node != null && node[GraphQLFields.Cursor] == null && edge[GraphQLFields.Cursor] is JsonValue cursorJsonValue)
                        node.Add(GraphQLFields.Cursor, cursorJsonValue.ToString());

                    //TODO: It is possible to add Arbitrary fields to the Edge that may be lost so we may want to add those to the Node here in the future . . . but may need to be a configurable option somewhere?

                    return node;
                });
        }

        protected PaginationType? DeterminePaginationType(JsonNode json)
        {
            if (json is JsonObject jsonObject)
            {
                //Cursor Paging uses [edge] or [nodes] array...
                if (jsonObject.ContainsKey(GraphQLFields.Nodes) || jsonObject.ContainsKey(GraphQLFields.Edges))
                    return PaginationType.Cursor;
                //Offset (aka CollectionSegment) Paging always uses [items] array...
                else if (jsonObject.ContainsKey(GraphQLFields.Items))
                    return PaginationType.Offset;
            }

            return null;

        }
    }
}
