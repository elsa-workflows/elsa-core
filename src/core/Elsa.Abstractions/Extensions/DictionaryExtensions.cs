using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa
{
    public static class DictionaryExtensions
    {
        public static T GetItem<T>(this IDictionary<string, T> dictionary, string key, Func<T> defaultValue) => dictionary.ContainsKey(key) ? dictionary[key] : dictionary[key] = defaultValue();
        public static T? GetItem<T>(this IDictionary<string, T> dictionary, string key) => dictionary.ContainsKey(key) ? dictionary[key] : default;

        public static void SetItem<T>(this IDictionary<string, T> dictionary, string key, T? value)
        {
            if (value == null)
            {
                if (dictionary.ContainsKey(key))
                    dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = value!;
            }
        }

        public static void Prune<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            var keys = dictionary.Where(predicate).Select(x => x.Key);
            foreach (var key in keys) 
                dictionary.Remove(key);
        }
    }
}