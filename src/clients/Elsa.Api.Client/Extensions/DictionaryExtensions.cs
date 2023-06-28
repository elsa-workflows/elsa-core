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
        if (dictionary.TryGetValue(key, out var value) && value is not JsonElement { ValueKind: JsonValueKind.Undefined }) 
            return value.ConvertTo<T>(new ObjectConverterOptions(serializerOptions));
        
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
        if (dictionary.TryGetValue(key, out var value) && value is not JsonElement { ValueKind: JsonValueKind.Undefined }) 
            return value;
        
        if (defaultValue == null)
            return default;

        var defaultVal = defaultValue()!;
        dictionary[key] = defaultVal;
        return defaultVal;
    }
}