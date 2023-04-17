using Elsa.Expressions.Helpers;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class DictionaryExtensions
{
    public static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T value) => dictionary.TryGetValue<string, T>(key, out value);
    public static bool TryGetValue<T>(this IDictionary<object, object> dictionary, string key, out T value) => dictionary.TryGetValue<object, T>(key, out value);

    public static bool TryGetValue<TKey, T>(this IDictionary<TKey, object> dictionary, TKey key, out T value)
    {
        if (!dictionary.TryGetValue(key, out var item))
        {
            value = default!;
            return false;
        }

        value = ConvertValue<T>(item);
        return true;
    }

    public static T? GetValue<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key) => ConvertValue<T>(dictionary[key]);
    public static T? GetValue<T>(this IDictionary<string, object> dictionary, string key) => ConvertValue<T>(dictionary[key]);
    
    public static T? GetValueOrDefault<TKey, T>(this IDictionary<TKey, object> dictionary, TKey key, Func<T?> defaultValueFactory) => TryGetValue<TKey, T>(dictionary, key, out var value) ? value : defaultValueFactory();
    public static T? GetValueOrDefault<TKey, T>(this IDictionary<TKey, object> dictionary, TKey key) => GetValueOrDefault<TKey, T>(dictionary, key, () => default);
    public static T? GetValueOrDefault<T>(this IDictionary<string, object> dictionary, string key, Func<T?> defaultValueFactory) => TryGetValue<T>(dictionary, key, out var value) ? value : defaultValueFactory();
    public static T? GetValueOrDefault<T>(this IDictionary<string, object> dictionary, string key) => GetValueOrDefault<T>(dictionary, key, () => default);
    public static object? GetValueOrDefault(this IDictionary<string, object> dictionary, string key) => GetValueOrDefault<object>(dictionary, key, () => default);

    public static T GetOrAdd<T>(this IDictionary<string, object> dictionary, string key, Func<T> valueFactory) where T : notnull => dictionary.GetOrAdd<string, T>(key, valueFactory);

    public static T GetOrAdd<TKey, T>(this IDictionary<TKey, object> dictionary, TKey key, Func<T> valueFactory)
    {
        if (dictionary.TryGetValue<TKey, T>(key, out var value))
            return value!;

        value = valueFactory()!;
        dictionary.Add(key, value);
        return value;
    }

    public static IDictionary<string, object> AddInput<T>(this IDictionary<string, object> dictionary, T value) where T : notnull => dictionary.AddInput(typeof(T).Name, value);

    public static IDictionary<string, object> AddInput(this IDictionary<string, object> dictionary, string key, object value)
    {
        dictionary.Add(key, value);
        return dictionary;
    }

    private static T? ConvertValue<T>(object? value) => value.ConvertTo<T>();
}