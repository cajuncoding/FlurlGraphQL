using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlurlGraphQL.CustomExtensions;

namespace FlurlGraphQL.ReflectionExtensions
{
    internal static class ReflectionExtensions
    {
        /// <summary>
        /// BBernard
        /// A wonderful little utility for robust Generic Type comparisons 
        /// Adapted from GraphQL ResolverExtensions Open Source library here:
        /// https://github.com/cajuncoding/GraphQL.RepoDB/blob/main/GraphQL.ResolverProcessingExtensions/DotNetCustomExtensions/TypeCustomExtensions.cs
        /// Also For more info see: https://stackoverflow.com/a/37184228/7293142
        ///  </summary>
        /// <param name="type"></param>
        /// <param name="parentType"></param>
        /// <returns></returns>
        public static bool IsDerivedFromGenericParent(this Type type, Type parentType)
        => (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == parentType)
           || type.BaseType.IsDerivedFromGenericParent(parentType)
           //Recursively search for Interfaces...
           || type.GetInterfaces().Any(t => t.IsDerivedFromGenericParent(parentType));

        public static bool InheritsFrom<TInterface>(this Type type)
            => typeof(TInterface).IsAssignableFrom(type);

        public static bool InheritsFrom(this Type type, Type baseType)
            => baseType.IsAssignableFrom(type);

        ///// <summary>
        ///// BBernard
        ///// Convenience method for getting a private/protected Field Info for an object instance with brute force reflection...
        ///// NOTE: Adapted from the Stack Overflow post: https://stackoverflow.com/a/16136854/7293142
        ///// </summary>
        //public static Func<TClass, TField> CreateGetFieldDelegate<TClass, TField>(this Type type, string fieldName)
        //{
        //    var typeExpr = Expression.Parameter(type);
        //    var fieldExpr = Expression.Field(typeExpr, fieldName);
        //    return Expression.Lambda<Func<TClass, TField>>(fieldExpr, typeExpr).Compile();
        //}

        /// <summary>
        /// BBernard
        /// Convenience method for getting a private/protected Property Value of an object instance with brute force reflection...
        /// NOTE: This is not the highest performance mechanism for doing this because Reflection is always used and the MethodInfo is not cached!
        ///         Therefore care should be taken to avoid using it in a tight loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T BruteForceGetFieldValue<T>(this object obj, string name)
        {
            FieldInfo field = obj?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return (T)field.GetValue(obj);

            return default;
        }

        /// <summary>
        /// BBernard
        /// Convenience method for getting a private/protected Property Value of an object instance with brute force reflection...
        /// NOTE: This is not the highest performance mechanism for doing this because Reflection is always used and the MethodInfo is not cached!
        ///         Therefore care should be taken to avoid using it in a tight loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T BruteForceGetPropertyValue<T>(this object obj, string name)
        {
            PropertyInfo prop = obj?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (prop != null)
                return (T)prop.GetValue(obj);

            return default;
        }

        public static Type FindType(this AppDomain appDomain, string className, string assemblyName = null, string namespaceName = null)
            => appDomain.GetAssemblies()
                .Where(a => assemblyName == null || (a.GetName().Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) ?? false))
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.Namespace != null
                    && (namespaceName == null || t.Namespace.Equals(namespaceName, StringComparison.OrdinalIgnoreCase))
                    && t.Name.Equals(className, StringComparison.OrdinalIgnoreCase)
                );

        private const BindingFlags DefaultInstanceOrStaticOrPublicOrPrivate = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static TDelegate CreateDelegateForMethod<TDelegate>(this Type type, string methodName, BindingFlags bindingFlags = DefaultInstanceOrStaticOrPublicOrPrivate, object objectToBindTo = null)
            where TDelegate : Delegate
        {
            var methodInfo = type?.GetMethod(methodName, bindingFlags);
            return methodInfo != null
                ? Delegate.CreateDelegate(typeof(TDelegate), objectToBindTo, methodInfo) as TDelegate
                : null;
        }
    }

    internal static class FindAttributeReflectionExtensions
    {
        public static IEnumerable<Attribute> FindAttributes(this Type type, params string[] attributeNames)
        {
            if (type == null)
                return null;

            var attributes = type.GetCustomAttributes(true).OfType<Attribute>();
            return FindAttributes(attributes, attributeNames);
        }

        public static IEnumerable<Attribute> FindAttributes(this PropertyInfo propInfo, params string[] attributeNames)
        {
            if (propInfo == null)
                return null;

            var attributes = propInfo.GetCustomAttributes(true).OfType<Attribute>();
            return FindAttributes(attributes, attributeNames);
        }

        public static IEnumerable<Attribute> FindAttributes(this IEnumerable<Attribute> attributes, params string[] attributeNamesToFind)
        {

            if (attributeNamesToFind.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(attributeNamesToFind));

            var results = new List<Attribute>();
            var attributesArray = attributes as Attribute[] ?? attributes.AsArray();

            foreach (var findName in attributeNamesToFind)
            {
                var findAttrName = findName.EndsWith(nameof(Attribute), StringComparison.OrdinalIgnoreCase)
                    ? findName
                    : string.Concat(findName, nameof(Attribute));

                var foundAttr = attributesArray.FirstOrDefault(attr => attr.GetType().Name.Equals(findAttrName, StringComparison.OrdinalIgnoreCase));
                if (foundAttr != null)
                    results.Add(foundAttr);
            }

            return results;
        }

    }
}
