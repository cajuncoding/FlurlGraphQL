﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using FlurlGraphQL.CustomExtensions;
using FlurlGraphQL.ValidationExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlurlGraphQL.JsonProcessing
{
    internal class FlurlGraphQLNewtonsoftJsonTransformer : IFlurlGraphQLJsonTransfomer<JToken>
    {
        #region Factory Methods
        
        public static FlurlGraphQLNewtonsoftJsonTransformer ForType<TEntityRoot>() 
            => new FlurlGraphQLNewtonsoftJsonTransformer(FlurlGraphQLJsonTransformTypeInfo.ForType<TEntityRoot>());

        #endregion

        public FlurlGraphQLJsonTransformTypeInfo JsonTransformTypeInfo { get; }
        
        protected FlurlGraphQLNewtonsoftJsonTransformer(FlurlGraphQLJsonTransformTypeInfo jsonTransformTypeInfo)
        {
            JsonTransformTypeInfo = jsonTransformTypeInfo.AssertArgIsNotNull(nameof(jsonTransformTypeInfo));
        }
        
        /// <summary>
        /// Transforms the GraphQL Json results to flatten/simplify the hierarchy and support simplified/easier model mapping so that domain
        ///     models do not need to be polluted with GraphQL specific things like edges, nodes, items, etc.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public (JToken Json, PaginationType? PaginationType) TransformJsonForSimplifiedGraphQLModelMapping(JToken json)
        {
            if (json == null) return (null, null);

            if (Debugger.IsAttached)
                Debug.WriteLine($"[{nameof(FlurlGraphQLNewtonsoftJsonTransformer)}] Original GraphQL Json:{Environment.NewLine}{json.ToString(Formatting.Indented)}");

            //NOTE: We really only need to know the Pagination Type of the Parent/Root node so that we can correctly determine the proper
            //          type of Paginated results (e.g. ConnectionResults vs CollectionSegmentResults) for the FlurlGraphQL root results type
            //          which nearly always is a simple List<> of Models, so we wrap them in our Results at the top level.
            //      All others will be defined as core types of the base model implementing IFlurlGraphQLQueryResults or any one of the derived
            //          default classes which can be directly de-serialized to.
            var paginationType = DeterminePaginationType(json);
            var processedJson = json;

            //If our ROOT EntityType does not implement IGraphQLResults then we need to Flatten the Root Level first...
            //  This ensures we are accessing the expected data within the [edges], [nodes], or [items] collection for all recursive processing...
            if(!this.JsonTransformTypeInfo.ImplementsIGraphQLQueryResults && json is JObject jsonObject)
                processedJson = TransformGraphQLJsonObjectAsNeeded(jsonObject, this.JsonTransformTypeInfo.ImplementsIGraphQLEdge);

            //Secondly, after the Root is prepared we can recursively process the rest of the Json...
            var rewrittenJson = TransformGraphQLJsonAsNeededRecursively(processedJson);

            if (Debugger.IsAttached)
                Debug.WriteLine($"[{nameof(FlurlGraphQLNewtonsoftJsonTransformer)}] Rewritten GraphQL Json:{Environment.NewLine}{rewrittenJson.ToString(Formatting.Indented)}");

            return (rewrittenJson, paginationType);
        }

        /// <summary>
        /// Transform the Json as needed (and only where needed) to simplify the mapping of the Json to the provided Entity Model!
        ///     This will flatten complex GraphQL hierarchies such as [edges], [nodes], [items], etc. into simplified models as needed.
        /// The specified Entity Model defines what nodes are re-written as we only mutate the nodes necessary to attempt to match to the simplified model
        ///     and don't do any further processing other than what is defined in the JsonTransformTypeInfo which is built from the Entity Model Class structure!
        ///
        /// NOTE: When rewriting our Json we ONLY need to handle the Model properties that should be Flattened; meaning they implement ICollection
        ///          but do not implement our convenience/wrapper classes that derive from IGraphQLQueryResults (which can be directly de-serialized without rewriting).
        ///      Therefore this dramatically reduces the number and types of Json fields we need to look at (and conceptually maps to the original FlurlGraphQL v1.x logic
        ///          using the Newtonsoft JsonConverter approach which only ran on ICollection property types).
        /// </summary>
        /// <param name="json"></param>
        /// <param name="recursiveTransformPropInfos"></param>
        /// <returns></returns>
        private JToken TransformGraphQLJsonAsNeededRecursively  (JToken json, IList<FlurlGraphQLJsonTransformPropInfo> recursiveTransformPropInfos = null)
        {
            //If not currently processing recursively then we default to the Root Properties of the JsonTransformTypeInfo...
            var propsToTransform = recursiveTransformPropInfos ?? this.JsonTransformTypeInfo.ChildPropertiesToTransform;

            JToken finalJson = json;
            
            //NOTE: For Performance we ONLY process IF we actually have any properties to process otherwise we short circuit as there is no work to do!!!
            if (propsToTransform.HasAny())
            {
                switch (json)
                {
                    case JArray jsonArray:
                        finalJson = TransformGraphQLJsonArrayAsNeededRecursively(jsonArray, propsToTransform);
                        break;
                    case JObject jsonObject:
                        finalJson = TransformGraphQLJsonObjectAsNeededRecursively(jsonObject, propsToTransform);
                        break;
                }
            }

            //Finally return our re-written/mutated json!
            return finalJson;
        }

        private JArray TransformGraphQLJsonArrayAsNeededRecursively(JArray jsonArray, IList<FlurlGraphQLJsonTransformPropInfo> propsToTransform)
        {
            //Iterate and recursively process all items in the Array as needed...
            var rewrittenJsonItems = jsonArray.Select(jsonItem => 
                (object)TransformGraphQLJsonAsNeededRecursively(jsonItem, propsToTransform)
            ).ToArray();

            //NOTE: Clearing the Parent is not needed with Newtonsoft.Json so we save a little work here...
            //jsonArray.Clear();
            return new JArray(rewrittenJsonItems);
        }

        private JObject TransformGraphQLJsonObjectAsNeededRecursively(JObject jsonObject, IList<FlurlGraphQLJsonTransformPropInfo> propsToTransform)
        {
            foreach (var transformPropInfo in propsToTransform)
            {
                //NOTE: If for some reason the Property Node is not found then this will be null and further processing will be skipped
                //          as the Case statement won't match anything...
                var jsonPropNode = jsonObject.Field(transformPropInfo.PropertyMappedJsonName);
                JToken jsonUpdatedPropNode = null;

                switch (jsonPropNode)
                {
                    //Ensure this is Null safe and Fast by skipping anytime the Json is not a valid Object containing a [nodes], [items], or [edges] property...
                    case JArray jsonPropArray:
                        //If the value is an Array then we need to again recursively process all items in the Array as needed...
                        jsonUpdatedPropNode = TransformGraphQLJsonArrayAsNeededRecursively(jsonPropArray, propsToTransform);
                        break;
                    case JObject jsonPropObject:
                        //First transform the current Property Node... 
                        var rewrittenJsonPropObject = TransformGraphQLJsonObjectAsNeeded(jsonPropObject, transformPropInfo.ImplementsIGraphQLEdge);

                        //Then Recursively Transform all child Json Properties as needed and update our Final Json with the Results!!!
                        //NOTE: WE call back up the generic handler because we don't know if each property is a nested Array or Object so this keeps
                        //          the recursive processing going handling all types consistently...
                        jsonUpdatedPropNode = TransformGraphQLJsonAsNeededRecursively(rewrittenJsonPropObject, transformPropInfo.ResolveChildPropertiesToTransform());
                        break;
                }

                //Finally (if updated) then we need to update the fully/recursively rewritten Value on the Parent!
                if (jsonPropNode != null && jsonUpdatedPropNode != null)
                    jsonPropNode.Replace(jsonUpdatedPropNode);
            }

            return jsonObject;
        }

        private JToken TransformGraphQLJsonObjectAsNeeded(JObject json, bool isIGraphQLEdgeImplementedOnProp)
        {
            IEnumerable<JObject> entityNodes;

            //Dynamically resolve the Results from:
            // - the Nodes child of the Data Result (for nodes{} based Cursor Paginated queries)
            // - the Items child of the Data Result (for items{} based Offset Paginated queries)
            // - the Edges->Node child of the the Data Result (for Edges based queries that provide access to the Cursor)
            // - finally use the (non-nested) array of results if not a Paginated result set of any kind above...
            if (json.Field(GraphQLFields.Nodes) is JArray nodesJson)
            {
                entityNodes = isIGraphQLEdgeImplementedOnProp
                    //Handle the edge case (pun intended) where the GraphQLEdge<T> is used though only nodes{} was queried in the GraphQL query;
                    //  in this case we must map the Node to an Edge structure (with null cursor) for proper de-serialization.
                    ? ConvertGraphQLNodesToEdgesJsonArray(nodesJson)
                    : nodesJson.OfType<JObject>();
                
                //NOTE: Clearing the Parent is not needed with Newtonsoft.Json so we save a little work here...
                //nodesJson.Clear();
            }
            else if (json.Field(GraphQLFields.Items) is JArray itemsJson)
            {
                entityNodes = itemsJson.OfType<JObject>();
                //NOTE: Clearing the Parent is not needed with Newtonsoft.Json so we save a little work here...
                //itemsJson.Clear();
            }
            //Handle Edges case (which allow access to the Cursor)
            else if (json.Field(GraphQLFields.Edges) is JArray edgesJson)
            {
                //Handle case where GraphQLEdge<TNode> wrapper class is used to simplify retrieving the Edges (maintaining the more complex GraphQL model).
                //  Otherwise we simplify/flatten the edge hierarchy into an Array of Objects for simplified model mapping/deserialization!
                entityNodes = isIGraphQLEdgeImplementedOnProp
                    ? edgesJson.OfType<JObject>()
                    : FlattenGraphQLEdgesToJsonArray(edgesJson);

                //NOTE: Clearing the Parent is not needed with Newtonsoft.Json so we save a little work here...
                //We don't actually need to Clear the Edges (so we save a little work here) because the parent of each item was actually the [node] child within the [edge]!
                //edgesJson.Clear();
            }
            else
            {
                return json;
            }    

            var rewrittenJsonArray = new JArray(entityNodes.Cast<object>().ToArray());
            return rewrittenJsonArray;
        }

        private IEnumerable<JObject> FlattenGraphQLEdgesToJsonArray(JArray edgesArray)
        {
            return edgesArray
                .OfType<JObject>()
                .Select(edge =>
                {
                    var node = edge.Field(GraphQLFields.Node) as JObject;

                    if (node != null)
                    {
                        ////NOW we must MOVE / Re-locate all Nodes into our output JArray which means we have to remove them from the Parent to keep our final Json clean...
                        edge.Remove(GraphQLFields.Node);

                        //Migrate all Properties from the Edge level to the Node level so they can more easily be mapped into C# Models
                        //  by simply adding corresponding properties; the main use case for this is likely the Cursor property of the Edge...
                        //We do check to ensure that a Json property of the same name doesn't already exist to ensure we don't incorrectly overwrite it!
                        //NOTE: For example with the Cursor, now any consumer can just add a "Cursor" property to their model to get the node's cursor as it's been flattened into the node itself.
                        //NOTE: We don't need to worry about the actual Node property here since it was already removed above (it MUST be removed for other reasons)...
                        var edgePropsToMove = edge.Properties().Where(p => !node.ContainsKey(p.Name)).ToArray();

                        //We must FIRST Remove the Props from the Edge to remove them from the Parent to keep our final Json clean;
                        //  clearing the Edge seems to be the easiest/fastest approach because our Simplified Model will not handle any properties
                        //  at the edge level anyway hence we attempt to re-locate them into the Model level of the Json...
                        if (edgePropsToMove.Length > 0)
                            edge.RemoveAll();

                        foreach (var edgeProp in edgePropsToMove)
                        {
                            var propValue = edgeProp.Value;
                            if (propValue.Type != JTokenType.Null && propValue.Type != JTokenType.Undefined)
                                node.Add(edgeProp.Name, edgeProp.Value);
                        }
                    }

                    return node;
                })
                .Where(i => i != null);
        }

        private IEnumerable<JObject> ConvertGraphQLNodesToEdgesJsonArray(JArray nodesArray)
        {
            return nodesArray
                //NOW re-map the node objects into an Edge Structure for proper de-serialization (e.g. when GraphQLEdge<T> but only nodes {} are requested)...
                //When converting from a Node, we don't have a Cursor property so it is simply null, but we are building the structure for proper de-serialization to GraphQLEdge<T>...
                //NOTE: For performance we set the values directly as they are expected (vs serializing an anonymous object which would have a lot more overhead)...
                .Select(node => new JObject
                {
                    [GraphQLFields.Cursor] = null,
                    [GraphQLFields.Node] = node
                });
        }

        private PaginationType? DeterminePaginationType(JToken json)
        {
            if (json is JObject jsonObject)
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
