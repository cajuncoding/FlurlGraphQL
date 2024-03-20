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
        
        public static FlurlGraphQLJsonRewriterTypeInfo ForType(Type entityType)
        {
            var jsonTypeInfoLazy = JsonTypeInfoCache.GetOrAdd(entityType, new Lazy<FlurlGraphQLJsonRewriterTypeInfo>(
                () =>
                {
                    var rewriterTypeInfo = new FlurlGraphQLJsonRewriterTypeInfo(entityType);
                    return rewriterTypeInfo;
                }));

            return jsonTypeInfoLazy.Value;
        }

        #endregion

        protected FlurlGraphQLJsonRewriterTypeInfo(Type targetType)
        {
            var entityType = targetType.AssertArgIsNotNull(nameof(targetType));
            if (targetType.IsGenericType)
                entityType = targetType.GenericTypeArguments.First();

            TypeName = targetType.Name;
            EntityTypeName = (entityType ?? targetType).Name;
            ImplementsIGraphQLQueryResults = entityType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
            AllChildProperties = GetPropInfosToRewriteForType(targetType);
            ChildPropertiesToRewrite = AllChildProperties.Where(p => p.ShouldBeFlattened).ToArray();
        }

        public string TypeName { get; }
        public string EntityTypeName { get; }
        public bool ImplementsIGraphQLQueryResults { get; }

        public IList<FlurlGraphQLJsonRewriterPropInfo> AllChildProperties { get; }
        public IList<FlurlGraphQLJsonRewriterPropInfo> ChildPropertiesToRewrite { get; }

        #region Internal Helpers

        protected IList<FlurlGraphQLJsonRewriterPropInfo> GetPropInfosToRewriteForType(Type targetType)
        {
            var entityType = targetType.IsGenericType
                ? targetType.GenericTypeArguments.First()
                : targetType.IsArray
                    ? targetType.GetElementType()
                    : targetType;

            var rewriterPropInfos = entityType?
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite && p.PropertyType.InheritsFrom(GraphQLTypeCache.ICollection))
                .Select(propInfo => new FlurlGraphQLJsonRewriterPropInfo(
                    propertyType: propInfo.PropertyType,
                    propertyName: propInfo.Name,
                    propertyMappedJsonName: ResolveMappedJsonPropertyName(propInfo)
                )).ToArray();
            
            return rewriterPropInfos ?? Array.Empty<FlurlGraphQLJsonRewriterPropInfo>();
        }

        /// <summary>
        /// Dynamically retrieves the mapped Json Property name regardless if System.Text.Json or Newtonsoft.Json mapping attributes are used.
        /// This allows us to simplify the code for either Json mapping implementation in one place. And all reflection impacts are mitigated
        ///     by caching of the final built Type Info results...
        /// </summary>
        /// <param name="propInfo"></param>
        /// <returns></returns>
        protected string ResolveMappedJsonPropertyName(PropertyInfo propInfo)
        {
            var mappingAttribute = propInfo.FindAttributes(
                SystemTextJsonConstants.JsonPropertyAttributeClassName,
                NewtonsoftJsonConstants.JsonPropertyAttributeClassName
            ).FirstOrDefault();

            switch (mappingAttribute?.GetType().Name)
            {
                case SystemTextJsonConstants.JsonPropertyAttributeClassName:
                    return mappingAttribute.BruteForceGetPropertyValue<string>(SystemTextJsonConstants.JsonPropertyAttributeNamePropertyName);
                case NewtonsoftJsonConstants.JsonPropertyAttributeClassName:
                    return mappingAttribute.BruteForceGetPropertyValue<string>(NewtonsoftJsonConstants.JsonPropertyAttributeNamePropertyName);
                default:
                    return propInfo.Name;
            }
        }

        #endregion
    }

    public class FlurlGraphQLJsonRewriterPropInfo
    {
        public FlurlGraphQLJsonRewriterPropInfo(Type propertyType, string propertyName, string propertyMappedJsonName)
        {
            PropertyType = propertyType.AssertArgIsNotNull(nameof(propertyType));
            PropertyName = propertyName.AssertArgIsNotNullOrBlank(nameof(propertyName));
            PropertyMappedJsonName = propertyMappedJsonName.AssertArgIsNotNullOrBlank(nameof(propertyMappedJsonName));

            ImplementsICollection = propertyType.InheritsFrom(GraphQLTypeCache.ICollection);
            ImplementsIGraphQLQueryResults = propertyType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLQueryResultsType);
            ImplementsIGraphQLEdge = propertyType.IsDerivedFromGenericParent(GraphQLTypeCache.IGraphQLEdgeEntityType);
        }

        public Type PropertyType { get; }
        public string PropertyName { get; }
        public string PropertyMappedJsonName { get; }
        public bool ImplementsICollection { get; }
        public bool ImplementsIGraphQLQueryResults { get; }
        public bool ImplementsIGraphQLEdge { get; }
        //We should ONLY Flatten Properties that are Collections but that do NOT implement our own IGraphQLResults interface
        //  which can be deserialized without flattening/rewriting the results structure!
        public bool ShouldBeFlattened => ImplementsICollection && !ImplementsIGraphQLQueryResults;

        public IList<FlurlGraphQLJsonRewriterPropInfo> ResolveChildPropertiesToRewrite()
            => FlurlGraphQLJsonRewriterTypeInfo.ForType(PropertyType)?.ChildPropertiesToRewrite 
                ?? Array.Empty<FlurlGraphQLJsonRewriterPropInfo>();

        public override string ToString() => $"Prop=[{PropertyName}]::JsonName[{PropertyMappedJsonName}]";
    }
}
