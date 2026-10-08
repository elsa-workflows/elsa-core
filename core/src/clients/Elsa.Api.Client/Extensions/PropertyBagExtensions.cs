using System.Text.Json;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for the PropertyBag class.
/// </summary>
public static class PropertyBagExtensions
{
    /// <summary>
    /// Tries to retrieve a value from the PropertyBag based on the provided key.
    /// If the specified key does not exist in the PropertyBag, the method will return the default value obtained from the defaultValue function.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="propertyBag">The PropertyBag to retrieve the value from.</param>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="defaultValue">A function that returns the default value to be returned if the key does not exist in the PropertyBag.</param>
    /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
    public static T? TryGetValueOrDefault<T>(this PropertyBag propertyBag, string key, Func<T> defaultValue)
    {
        if (!propertyBag.TryGetValue(key, out var value))
            return defaultValue();

        var json = value.ToString();

        if (string.IsNullOrWhiteSpace(json))
            return defaultValue();

        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Sets a value in the PropertyBag based on the provided key.
    /// The value is serialized using JSON.
    /// </summary>
    /// <param name="propertyBag">The PropertyBag to set the value in.</param>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store in the PropertyBag.</param>
    public static void SetValue(this PropertyBag propertyBag, string key, object value)
    {
        var json = JsonSerializer.Serialize(value);
        propertyBag[key] = json;
    }
}