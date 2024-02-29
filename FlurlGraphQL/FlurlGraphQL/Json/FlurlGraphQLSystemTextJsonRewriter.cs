using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FlurlGraphQL.CustomExtensions;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL.FlurlGraphQL.Json
{
    internal class FlurlGraphQLSystemTextJsonRewriter
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
            var processedJson = json;

            //If our ROOT EntityType does not implement IGraphQLResults then we need to Flatten the Root Level first...
            //  This ensures we are accessing the expected data within the [edges], [nodes], or [items] collection for all recursive processing...
            if(!JsonRewriterTypeInfo.ImplementsIGraphQLQueryResults && json is JsonObject jsonObject)
                processedJson = RewriteGraphQLJsonObjectAsNeeded(jsonObject, false);

            var rewrittenJson = RewriteGraphQLJsonAsNeededRecursively(processedJson);

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
        private JsonNode RewriteGraphQLJsonAsNeededRecursively(JsonNode json, IList<FlurlGraphQLJsonRewriterPropInfo> recursiveRewriterPropInfos = null)
        {
            //If not currently processing recursively then we default to the Root Properties of the JsonRewriterTypeInfo...
            var propsToRewrite = recursiveRewriterPropInfos ?? this.JsonRewriterTypeInfo.ChildPropertiesToRewrite;

            JsonNode finalJson = json;
            
            //NOTE: For Performance we ONLY process IF we actually have any properties to process otherwise we short circuit as there is no work to do!!!
            if (propsToRewrite.HasAny())
            {
                switch (json)
                {
                    case JsonArray jsonArray:
                        finalJson = RewriteGraphQLJsonArrayAsNeededRecursively(jsonArray, propsToRewrite);
                        break;
                    case JsonObject jsonObject:
                    {
                        finalJson = RewriteGraphQLJsonObjectAsNeededRecursively(jsonObject, propsToRewrite);
                        break;
                    }
                }
            }

            //Finally return our re-written/mutated json!
            return finalJson;
        }


        private JsonArray RewriteGraphQLJsonArrayAsNeededRecursively(JsonArray jsonArray, IList<FlurlGraphQLJsonRewriterPropInfo> propsToRewrite)
        {
            //Iterate and recursively process all items in the Array as needed...
            var rewrittenJsonItems = jsonArray.Select(jsonItem =>
            {
                var rewrittenJson = RewriteGraphQLJsonAsNeededRecursively(jsonItem, propsToRewrite);
                return rewrittenJson;
            }).ToArray();

            jsonArray.Clear();
            return new JsonArray(rewrittenJsonItems);
        }

        private JsonObject RewriteGraphQLJsonObjectAsNeededRecursively(JsonObject jsonObject, IList<FlurlGraphQLJsonRewriterPropInfo> propsToRewrite)
        {
            JsonObject finalJson = jsonObject;

            foreach (var rewriterPropInfo in propsToRewrite)
            {
                //NOTE: If for some reason the Property Node is not found then this will be null and further processing will be skipped
                //          as the Case statement won't match anything...
                var jsonPropNode = jsonObject[rewriterPropInfo.PropertyMappedJsonName];

                switch (jsonPropNode)
                {
                    //Ensure this is Null safe and Fast by skipping anytime the Json is not a valid Object containing a [nodes], [items], or [edges] property...
                    case JsonArray jsonPropArray:
                        //If the value is an Array then we need to again recursively process all items in the Array as needed...
                        jsonPropNode = RewriteGraphQLJsonArrayAsNeededRecursively(jsonPropArray, propsToRewrite);
                        break;
                    case JsonObject jsonPropObject:
                        //First rewrite the current Property Node... 
                        var rewrittenJsonPropObject = RewriteGraphQLJsonObjectAsNeeded(jsonPropObject, rewriterPropInfo.ImplementsIGraphQLEdge);

                        //Then Recursively Rewrite all child Json Properties as needed and update our Final Json with the Results!!!
                        //NOTE: WE call back up the generic handler because we don't know if each property is a nested Array or Object so this keeps
                        //          the recursive processing going handling all types consistently...
                        jsonPropNode = RewriteGraphQLJsonAsNeededRecursively(rewrittenJsonPropObject, rewriterPropInfo.ChildPropertiesToRewrite);
                        break;
                }

                //Finally (if updated) then we need to update the fully/recursively rewritten Value on the Parent!
                if (jsonPropNode != null)
                {
                    jsonObject[rewriterPropInfo.PropertyMappedJsonName] = jsonPropNode;
                    finalJson = jsonObject;
                }
            }

            return finalJson;
        }

        private JsonArray RewriteGraphQLJsonObjectAsNeeded(JsonObject json, bool isIGraphQLEdgeImplementedOnProp)
        {
            IList<JsonObject> entityNodes = Array.Empty<JsonObject>();

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            //NOTE: When relocating the nodes we must Clear the original parent Node to explicitly remove all children so that the Nodes can be re-assigned
            //          to a new location in the Json; as System.Text.Json will throw exceptions otherwise!
            if (json[GraphQLFields.Nodes] is JsonArray nodesJson)
            {
                entityNodes = nodesJson.OfType<JsonObject>().ToArray();
                nodesJson.Clear();
            }
            else if (json[GraphQLFields.Items] is JsonArray itemsJson)
            {
                entityNodes = itemsJson.OfType<JsonObject>().ToArray();
                itemsJson.Clear();
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json[GraphQLFields.Edges] is JsonArray edgesJson)
            {
                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges (maintaining the more complex GraphQL model).
                //  Otherwise we simplify/flatten the edge hierarchy into an Array of Objects for simplified model mapping/deserialization!
                entityNodes = isIGraphQLEdgeImplementedOnProp
                    ? edgesJson.OfType<JsonObject>().ToArray()
                    : FlattenGraphQLEdgesToJsonArray(edgesJson);

                //We don't actually need to Clear the Edges (so we save a little work here) because the parent of each item was actually the [node] child within the [edge]!
                //edgesJson.Clear();
            }

            var rewrittenJsonArray = new JsonArray(entityNodes.OfType<JsonNode>().ToArray());
            return rewrittenJsonArray;
        }

        private IList<JsonObject> FlattenGraphQLEdgesToJsonArray(JsonArray edgesArray)
        {
            return edgesArray
                .OfType<JsonObject>()
                .Select(edge =>
                {
                    var node = edge[GraphQLFields.Node] as JsonObject;

                    if (node != null)
                    {
                        //NOW we must MOVE / Re-locate all Nodes into our output JsonArray which means we have to remove them from the Parent to avoid "Node already has a Parent" exceptions...
                        edge.Remove(GraphQLFields.Node);

                        //Migrate all Properties from the Edge level to the Node level so they can more easily be mapped into C# Models
                        //  by simply adding corresponding properties; the main use case for this is likely the Cursor property of the Edge...
                        //We do check to ensure that a Json property of the same name doesn't already exist to ensure we don't incorrectly overwrite it!
                        //NOTE: For example with the Cursor, now any consumer can just add a "Cursor" property to their model to get the node's cursor as it's been flattened into the node itself.
                        //NOTE: We don't need to worry about the actual Node property here since it was already removed above (it MUST be removed for other reasons)...
                        var edgePropsToMove = edge.Where(p => !node.ContainsKey(p.Key)).ToArray();

                        //We must FIRST Remove the Props from the Edge to remove them from the Parent to avoid "Node already has a Parent" exceptions;
                        //  clearing the Edge seems to be the easiest/fastest approach... 
                        if (edgePropsToMove.HasAny())
                            edge.Clear();

                        foreach (var edgeProp in edgePropsToMove)
                        {
                            if (edgeProp.Value is JsonValue edgePropJsonValue)
                                node.Add(edgeProp.Key, edgePropJsonValue);
                        }
                    }

                    return node;
                })
                .Where(i => i.IsNotNullOrUndefined())
                .ToArray();
        }

        private PaginationType? DeterminePaginationType(JsonNode json)
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
