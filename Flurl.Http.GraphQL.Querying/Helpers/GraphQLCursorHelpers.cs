using System;
using System.Text;

namespace Flurl.Http.GraphQL.Querying
{
    public class GraphQLCursorHelpers
    {
        /// <summary>
        /// Helper method to convert an ordinal int index value to an opaque cursor that is compatible with the MarketingEvents GraphQL API.
        /// </summary>
        /// <param name="ordinalIndex"></param>
        /// <returns></returns>
        public static string CreateCursorFromIndex(int ordinalIndex)
        {
            var indexString = ordinalIndex.ToString();
            var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(indexString));
            return cursor;
        }

        /// <summary>
        /// Helper to convert a provided GraphQL paging Cursor to an Index/Ordinal value; assuming it is an opaque cursor derived from Base64 encoding of the Index!
        /// If parsing fails then the result will be null.
        /// </summary>
        /// <param name="cursor"></param>
        /// <returns></returns>
        public static int? ParseIndexFromCursor(string cursor)
        {
            int? index = null;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                try
                {
                    var indexString = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                    if (int.TryParse(indexString, out int parsedIndex))
                        index = parsedIndex;
                }
                catch
                {
                    //DO NOTHING to be completely Null Safe!
                }
            }

            return index;
        }
    }
}
