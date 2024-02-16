using System;

namespace FlurlGraphQL
{
    internal static class JsonExtensions
    {
        public static bool IsDuckTypedJson(this string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                return false;

            var text = jsonText.Trim();
            return (text.StartsWith("{") && text.EndsWith("}")) //For object
                   || (text.StartsWith("[") && text.EndsWith("]")); //For array
        }
    }
}
