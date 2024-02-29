using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlurlGraphQL.ReflectionConstants;
using FlurlGraphQL.ReflectionExtensions;
using FlurlGraphQL.TypeCacheHelpers;
using FlurlGraphQL.ValidationExtensions;

namespace FlurlGraphQL.FlurlGraphQL.Json
{
    public class FlurlGraphQLJsonRewriterTypeInfo
    {
        #region Factory Methods with Caching

        protected static ConcurrentDictionary<Type, Lazy<FlurlGraphQLJsonRewriterTypeInfo>> JsonTypeInfoCache { get; }
            = new ConcurrentDictionary<Type, Lazy<FlurlGraphQLJsonRewriterTypeInfo>>();

        public static FlurlGraphQLJsonRewriterTypeInfo ForType<T>() => ForType(typeof(T));
        
        public static FlurlGraphQLJsonRewriterTypeInfo ForType(Type targetType)
        {
            var jsonTypeInfoLazy = JsonTypeInfoCache.GetOrAdd(targetType, new Lazy<FlurlGraphQLJsonRewriterTypeInfo>(
                () => new FlurlGraphQLJsonRewriterTypeInfo(targetType)
            ));

            return jsonTypeInfoLazy.Value;
        }

        #endregion

        public FlurlGraphQLJsonRewriterTypeInfo(Type targetType)
        {
            var entityType = targetType.AssertArgIsNotNull(nameof(targetType));
            if (targetType.IsGenericType)
                entityType = targetType.GenericTypeArguments.First();

            TypeName = targetType.Name;
            EntityTypeName = (entityType ?? targetType).Name;
            ImplementsIGraphQLQueryResults = entityType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
            AllChildProperties = BuildRewriterJsonPropInfosRecursivelyInternal(targetType);
            ChildPropertiesToRewrite = AllChildProperties.Where(p => p.ShouldBeFlattened).ToArray();
        }

        public string TypeName { get; }
        public string EntityTypeName { get; }
        public bool ImplementsIGraphQLQueryResults { get; }

        public IList<FlurlGraphQLJsonRewriterPropInfo> AllChildProperties { get; }
        public IList<FlurlGraphQLJsonRewriterPropInfo> ChildPropertiesToRewrite { get; }

        #region Internal Helpers

        protected static IList<FlurlGraphQLJsonRewriterPropInfo> BuildRewriterJsonPropInfosRecursivelyInternal(Type targetType)
        {
            var collectionProperties = targetType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanWrite && p.PropertyType.InheritsFrom(GraphQLTypeCache.ICollection));

            var rewriterPropInfos = collectionProperties.Select(propInfo =>
            {
                var propertyType = propInfo.PropertyType;

                //Construct our Final Rewrite Prop Info and recursively process any Child Properties...
                return new FlurlGraphQLJsonRewriterPropInfo(
                    propertyName: propInfo.Name,
                    propertyMappedJsonName: GetMappedJsonPropertyName(propInfo),
                    implementsICollection: true, //Always True per the above Linq Query...
                    implementsIGraphQLQueryResults: propertyType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType),
                    implementsIGraphQLEdge: propertyType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLEdgeEntityType),
                    //Recursively traverse and build Child Property Info...
                    childProperties: BuildRewriterJsonPropInfosRecursivelyInternal(propertyType)
                );
            }).ToArray();

            return rewriterPropInfos;
        }

        /// <summary>
        /// Dynamically retrieves the mapped Json Property name regardless if System.Text.Json or Newtonsoft.Json mapping attributes are used.
        /// This allows us to simplify the code for either Json mapping implementation in one place. And all reflection impacts are mitigated
        ///     by caching of the final built Type Info results...
        /// </summary>
        /// <param name="propInfo"></param>
        /// <returns></returns>
        protected static string GetMappedJsonPropertyName(PropertyInfo propInfo)
        {
            var mappingAttribute = propInfo.FindAttributes(
                SystemTextJsonConstants.JsonPropertyAttributeName,
                NewtonsoftJsonConstants.JsonPropertyAttributeName
            ).FirstOrDefault();

            switch (mappingAttribute?.GetType().Name)
            {
                case SystemTextJsonConstants.JsonPropertyAttributeName:
                    return mappingAttribute.BruteForceGetPropertyValue<string>(SystemTextJsonConstants.JsonPropertyAttributeNamePropertyName);
                case NewtonsoftJsonConstants.JsonPropertyAttributeName:
                    return mappingAttribute.BruteForceGetPropertyValue<string>(NewtonsoftJsonConstants.JsonPropertyAttributeNamePropertyName);
                default:
                    return propInfo.Name;
            }
        }

        #endregion
    }

    public class FlurlGraphQLJsonRewriterPropInfo
    {
        public FlurlGraphQLJsonRewriterPropInfo(
            string propertyName,
            string propertyMappedJsonName,
            bool implementsICollection,
            bool implementsIGraphQLQueryResults,
            bool implementsIGraphQLEdge,
            IList<FlurlGraphQLJsonRewriterPropInfo> childProperties = null
        )
        {
            PropertyName = propertyName.AssertArgIsNotNullOrBlank(nameof(propertyName));
            PropertyMappedJsonName = propertyMappedJsonName.AssertArgIsNotNullOrBlank(nameof(propertyMappedJsonName));
            ImplementsICollection = implementsICollection;
            ImplementsIGraphQLQueryResults = implementsIGraphQLQueryResults;
            ImplementsIGraphQLEdge = implementsIGraphQLEdge;
            AllChildProperties = childProperties ?? Array.Empty<FlurlGraphQLJsonRewriterPropInfo>();
            ChildPropertiesToRewrite = AllChildProperties.Where(p => p.ShouldBeFlattened).ToArray();
        }

        public string PropertyName { get; }
        public string PropertyMappedJsonName { get; }
        public bool ImplementsICollection { get; }
        public bool ImplementsIGraphQLQueryResults { get; }
        public bool ImplementsIGraphQLEdge { get; }
        //We should ONLY Flatten Properties that are Collections but that do NOT implement our own IGraphQLResults interface
        //  which can be deserialized without flattening/rewriting the results structure!
        public bool ShouldBeFlattened => ImplementsICollection && !ImplementsIGraphQLQueryResults;
        public IList<FlurlGraphQLJsonRewriterPropInfo> AllChildProperties { get; }
        public IList<FlurlGraphQLJsonRewriterPropInfo> ChildPropertiesToRewrite { get; }

        public override string ToString() => $"Prop=[{PropertyName}]::JsonName[{PropertyMappedJsonName}]";
    }
}
