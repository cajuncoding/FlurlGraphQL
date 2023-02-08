using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Flurl.Http.GraphQL.Querying.NewtonsoftJson
{
    public class GraphQLAdaptiveJsonContractResolver : DefaultContractResolver
    {
        public static readonly GraphQLAdaptiveJsonContractResolver Instance = new GraphQLAdaptiveJsonContractResolver();

        public GraphQLAdaptiveJsonContractResolver()
        {
        }

        //protected override JsonContract CreateContract(Type objectType)
        //{
        //    JsonContract contract = base.CreateContract(objectType);

        //    // this will only be called once and then cached
        //    if (typeof(IEnumerable).IsAssignableFrom(objectType))
        //    {
        //        contract.Converter = new GraphQLPaginatedResultsJsonConverter();
        //    }

        //    return contract;
        //}

        //protected override JsonArrayContract CreateArrayContract(Type objectType)
        //{
        //    //First Use default initialization of the Array Contract (for any IEnumerable property mapping)...
        //    var arrayContract = base.CreateArrayContract(objectType);

        //    //Now add our support for simplifying GraphQL Paginated Results into IEnumerable objects (flattening Nodes/Edges/Items/etc.)...
        //    if (arrayContract.Converter == null)
        //    {
        //        arrayContract.Converter = new GraphQLPaginatedResultsJsonConverter();
        //    }

        //    return arrayContract;
        //}

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            //First Use default initialization of the Array Contract (for any IEnumerable property mapping)...
            var objectContract = base.CreateObjectContract(objectType);

            //Now add our support for simplifying GraphQL Paginated Results into IEnumerable objects (flattening Nodes/Edges/Items/etc.)...
            if (objectContract.Converter == null)
            {
                objectContract.Converter = new GraphQLPaginatedResultsJsonConverter();
            }

            return objectContract;
        }
    }
}
