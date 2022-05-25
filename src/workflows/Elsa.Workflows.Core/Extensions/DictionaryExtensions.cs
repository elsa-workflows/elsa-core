using System.Text.Json;

namespace Elsa;

public static class DictionaryExtensions
{
    public static bool TryGetValue<T>(this IDictionary<string, object?> dictionary, string key, out T? value) => dictionary.TryGetValue<string, T>(key, out value);
    public static bool TryGetValue<T>(this IDictionary<object, object?> dictionary, string key, out T? value) => dictionary.TryGetValue<object, T>(key, out value);
    
    public static bool TryGetValue<TKey, T>(this IDictionary<TKey, object?> dictionary, TKey key, out T? value)
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

    public static T? GetOrAdd<T>(this IDictionary<string, object?> dictionary, string key, Func<T> valueFactory) where T : notnull => dictionary.GetOrAdd<string, T>(key, valueFactory);

    public static T? GetOrAdd<TKey, T>(this IDictionary<TKey, object?> dictionary, TKey key, Func<T> valueFactory)
    {
        if (dictionary.TryGetValue<TKey, T>(key, out var value))
            return value;

        value = valueFactory();
        dictionary.Add(key, value);
        return value;
    }

    private static T? ConvertValue<T>(object? value)
    {
        return value switch
        {
            null => default,
            T v => v,
            JsonElement jsonElement => jsonElement.Deserialize<T>()!,
            _ => throw new InvalidOperationException()
        };
    }
}