using Newtonsoft.Json.Linq;
using System;

namespace Flurl.Http.GraphQL.Querying
{
    internal static class NewtonsoftJsonExtensions
    {
        public static bool TryParseJObject(this string jsonText, out JToken json)
        {
            try
            {
                if (IsDuckTypedJson(jsonText))
                {
                    json = JObject.Parse(jsonText);
                    return true;
                }
            }
            catch (Exception)
            {
                //DO NOTHING
            }

            json = null;
            return false;
        }

        public static bool IsDuckTypedJson(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                return false;

            var text = jsonText.Trim();
            return (text.StartsWith("{") && text.EndsWith("}")) //For object
                   || (text.StartsWith("[") && text.EndsWith("]")); //For array
        }

        /// <summary>
        /// BBernard
        /// Safely retrieves the specified field of any type as JToken from the Json (JObject/JProperty) with case-insensitive matching. This method
        ///     enables working with dynamic Json, and field/property investigations much easier.
        /// NOTE: This is Exception safe, any property that does not exist will return null and can be efficiently
        ///     used along with null-coalesce (?.) as well as type checking (e.g. 'is SomeType typedVar').
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static JToken Field(this JToken json, string fieldName)
        {
            switch (json)
            {
                case JObject jObject:
                    return jObject.TryGetValue(fieldName, StringComparison.OrdinalIgnoreCase, out var fieldValue)
                        ? fieldValue
                        : null;

                case JProperty jProp:
                    //Extract the JObject value and process if matched...
                    return (jProp.Value as JObject)?.Field(fieldName);

                default:
                    return null;
            }
        }

        /// <summary>
        /// BBernard
        /// Safely retrieves the specified field as a JProperty from the Json (JObject/JProperty) with case-insensitive matching; 
        ///     a JProperty allows easy setting of the value and other manipulation of the token/node. In addition, this method
        ///     enables working with dynamic Json, and field/property investigations much easier.
        /// NOTE: This is Exception safe, any property that does not exist will return null and can be efficiently
        ///     used along with null-coalesce (?.) as well as type checking (e.g. 'is SomeType typedVar').
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static JProperty Prop(this JToken json, string fieldName)
        {
            switch (json)
            {
                case JObject jObject:
                    return jObject?.Property(fieldName, StringComparison.OrdinalIgnoreCase);

                case JProperty jProp:
                    //Extract the JObject value and process if matched...
                    return (jProp.Value as JObject)?.Prop(fieldName);

                default:
                    return null;
            }
        }
    }
}
