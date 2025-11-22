using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class DictionaryExtensions
{
    extension(IDictionary<string, object> dictionary)
    {
        public bool TryGetValue<T>(string key, out T value) => dictionary.TryGetValue<string, T>(key, out value);
        public bool TryGetValue<T>(IEnumerable<string> keys, out T value) => dictionary.TryGetValue<string, T>(keys, out value);
    }

    public static bool TryGetValue<T>(this IDictionary<object, object> dictionary, string key, out T value) => dictionary.TryGetValue<object, T>(key, out value);

    public static bool TryGetValue<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key, out T value)
    {
        if (!dictionary.TryGetValue(key, out var item))
        {
            value = default!;
            return false;
        }

        value = item;
        return true;
    }
    
    extension<TKey>(IDictionary<TKey, object> dictionary)
    {
        public bool TryGetValue<T>(TKey key, out T value)
        {
            if (!dictionary.TryGetValue(key, out var item))
            {
                value = default!;
                return false;
            }

            var result = TryConvertValue<T>(item);
            value = result.Success ? (T)result.Value! : default!;
            return result.Success;
        }

        public bool TryGetValue<T>(IEnumerable<TKey> keys, out T value)
        {
            foreach (var key in keys)
            {
                if (dictionary.TryGetValue(key, out var item))
                {
                    var result = TryConvertValue<T>(item);
                    value = result.Success ? (T)result.Value! : default!;
                    return result.Success;
                }    
            }
        
            value = default!;
            return false;
        }
    }

    public static T? GetValue<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key) => ConvertValue<T>(dictionary[key]);
    public static T? GetValue<T>(this IDictionary<string, object> dictionary, string key) => ConvertValue<T>(dictionary[key]);
    public static T? GetValueOrDefault<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key, Func<T?> defaultValueFactory) => TryGetValue(dictionary, key, out var value) ? value : defaultValueFactory();
    
    extension<TKey>(IDictionary<TKey, object> dictionary)
    {
        public T? GetValueOrDefault<T>(TKey key, Func<T?> defaultValueFactory) => TryGetValue<TKey, T>(dictionary, key, out var value) ? value : defaultValueFactory();
        public T? GetValueOrDefault<T>(TKey key) => GetValueOrDefault<TKey, T>(dictionary, key, () => default);
    }

    extension(IDictionary<string, object> dictionary)
    {
        public T? GetValueOrDefault<T>(string key, Func<T?> defaultValueFactory) => TryGetValue<T>(dictionary, key, out var value) ? value : defaultValueFactory();
        public T? GetValueOrDefault<T>(IEnumerable<string> keys, Func<T?> defaultValueFactory) => TryGetValue<T>(dictionary, keys, out var value) ? value : defaultValueFactory();
        public T? GetValueOrDefault<T>(string key) => GetValueOrDefault<T>(dictionary, key, () => default);
        public object? GetValueOrDefault(string key) => GetValueOrDefault<object>(dictionary, key, () => null);
    }

    public static T GetOrAdd<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key, Func<T> valueFactory)
    {
        if(dictionary.TryGetValue(key, out T? value))
           return value;

        value = valueFactory()!;
        dictionary.Add(key, value);
        return value;
    }
    
    public static T GetOrAdd<TKey, T>(this IDictionary<TKey, object> dictionary, TKey key, Func<T> valueFactory)
    {
        if (dictionary.TryGetValue<TKey, T>(key, out var value))
            return value!;

        value = valueFactory()!;
        dictionary.Add(key, value);
        return value;
    }

    extension(IDictionary<string, object> dictionary)
    {
        public IDictionary<string, object> AddInput<T>(T value) where T : notnull => dictionary.AddInput(typeof(T).Name, value);

        public IDictionary<string, object> AddInput(string key, object value)
        {
            dictionary.Add(key, value);
            return dictionary;
        }

        /// <summary>
        /// Merges the specified dictionary with the other dictionary.
        /// When a key exists in both dictionaries, the value in the other dictionary will overwrite the value in the specified dictionary.
        /// </summary>
        public void Merge(IDictionary<string, object> other)
        {
            foreach (var (key, value) in other)
                dictionary[key] = value;
        }
    }

    private static T? ConvertValue<T>(object? value) => value.ConvertTo<T>();
    
    private static Result TryConvertValue<T>(object? value)
    {
        return value.TryConvertTo<T>();
    }
}