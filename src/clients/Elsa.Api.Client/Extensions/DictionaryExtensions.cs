using System.Text.Json;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static T? TryGetValue<T>(this IDictionary<string, object> dictionary, string key, Func<T>? defaultValue = default, JsonSerializerOptions? serializerOptions = default)
    {
        var caseInsensitiveDictionary = ToCaseInsensitiveDictionary(dictionary);

        if (caseInsensitiveDictionary.TryGetValue(key, out var value) && value is not JsonElement { ValueKind: JsonValueKind.Undefined })
        {
            var convertedValue = value.ConvertTo<T>(new ObjectConverterOptions(serializerOptions));

            if (convertedValue != null)
                return convertedValue;
        }

        if (defaultValue == null)
            return default;

        var defaultVal = defaultValue()!;
        dictionary[key] = defaultVal;
        return defaultVal;
    }

    /// <summary>
    /// Returns the value of the specified property if it exists, otherwise the default value.
    /// </summary>
    public static object? TryGetValue(this IDictionary<string, object> dictionary, string key, Func<object>? defaultValue = default)
    {
        var caseInsensitiveDictionary = ToCaseInsensitiveDictionary(dictionary);

        if (caseInsensitiveDictionary.TryGetValue(key, out var value) && value is not JsonElement { ValueKind: JsonValueKind.Undefined })
            return value;

        if (defaultValue == null)
            return default;

        var defaultVal = defaultValue()!;
        dictionary[key] = defaultVal;
        return defaultVal;
    }

    // A method that de-duplicates the keys in the dictionary, taking into account that there may already be multiple keys that would be considered the same if the case was ignored.
    private static Dictionary<string, object> ToCaseInsensitiveDictionary(this IDictionary<string, object> dictionary)
    {
        var caseInsensitiveDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in dictionary.Keys)
        {
            if (caseInsensitiveDictionary.ContainsKey(key))
                continue;

            caseInsensitiveDictionary[key] = dictionary[key]!;
        }

        return caseInsensitiveDictionary;
    }
}