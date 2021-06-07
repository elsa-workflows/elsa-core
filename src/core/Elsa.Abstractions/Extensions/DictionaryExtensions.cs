using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Safely gets a value by key from the dictionary without throwing if the key does not exist.
        /// </summary>
        public static T GetItem<T>(this IDictionary<string, T> dictionary, string key, Func<T> defaultValue) => dictionary.ContainsKey(key) ? dictionary[key] : dictionary[key] = defaultValue();
        
        /// <summary>
        /// Safely gets a value by key from the dictionary without throwing if the key does not exist.
        /// </summary>
        public static object GetItem(this IDictionary<string, object> dictionary, string key, Func<object> defaultValue) => dictionary.ContainsKey(key) ? dictionary[key] : dictionary[key] = defaultValue();
        
        /// <summary>
        /// Safely gets a value by key from the dictionary without throwing if the key does not exist.
        /// </summary>
        public static T? GetItem<T>(this IDictionary<string, T> dictionary, string key) => dictionary.ContainsKey(key) ? dictionary[key] : default;

        /// <summary>
        /// Sets a value by key. If the value is null, the entry is removed, if any.  
        /// </summary>
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

        /// <summary>
        /// Removes all entries matching the specified predicate.
        /// </summary>
        public static void Prune<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            var keys = dictionary.Where(predicate).Select(x => x.Key);
            foreach (var key in keys) 
                dictionary.Remove(key);
        }
    }
}