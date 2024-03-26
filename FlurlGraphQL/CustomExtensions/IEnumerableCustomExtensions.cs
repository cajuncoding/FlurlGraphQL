using System.Collections.Generic;
using System.Linq;

namespace FlurlGraphQL.CustomExtensions
{
    internal static class IEnumerableCustomExtensions
    {
        public static bool HasAny<T>(this IEnumerable<T> items)
            => items != null && items.Any();

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
            => !items.HasAny();

        public static T[] AsArray<T>(this IEnumerable<T> items)
            => items is T[] itemArray ? itemArray : items.ToArray();
    }
}
