using Flurl.Util;
using System.Collections.Generic;
using Flurl;

namespace FlurlGraphQL.Querying
{
    internal static class DictionaryExtensions
    {
        #region Internal Dictionary / Object Bag Helpers

        public static void SetObjectBagItem(this IDictionary<string, object> dictionary, string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (value == null && nullValueHandling == NullValueHandling.Remove && dictionary.ContainsKey(name))
                dictionary.Remove(name);
            else
                dictionary[name] = value;
        }

        public static void SetObjectBagItems(IDictionary<string, object> dictionary, object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => dictionary.SetObjectBagItems(variables.ToKeyValuePairs(), nullValueHandling);

        public static void SetObjectBagItems(this IDictionary<string, object> dictionary, IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            //NOTE: Currently re-using the built in Flurl ToKeyValuePairs() extension method...
            foreach (var (key, value) in variables.ToKeyValuePairs())
                dictionary.SetObjectBagItem(key, value, nullValueHandling);
        }

        #endregion
    }
}
