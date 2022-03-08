using System.Text.Json;

namespace Elsa.Extensions;

public static class DictionaryExtensions
{
    public static bool TryGetValue<T>(this IReadOnlyDictionary<string, object> dictionary, string key, out T? value) => TryGetValue((IDictionary<string, object?>)dictionary, key, out value);

    public static bool TryGetValue<T>(this IDictionary<string, object?> dictionary, string key, out T? value)
    {
        if (!dictionary.TryGetValue(key, out var item))
        {
            value = default!;
            return false;
        }

        value = ConvertValue<T>(item);
        return true;
    }

    public static T? GetValue<T>(this IDictionary<string, object?> dictionary, string key)
    {
        return ConvertValue<T>(dictionary[key]);
    }

    public static T GetOrAdd<T>(this IDictionary<string, object?> dictionary, string key, Func<T> valueFactory) where T : notnull
    {
        if (dictionary.TryGetValue<T>(key, out var value))
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