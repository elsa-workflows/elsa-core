using System.Text.Json;
using Elsa.Common.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

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
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
    public static T TryGetValueOrDefault<T>(this PropertyBag propertyBag, string key, Func<T> defaultValue, JsonSerializerOptions? options = null)
    {
        if (!propertyBag.TryGetValue(key, out var value))
            return defaultValue();

        var json = (string)value;
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Tries to retrieve a value from the PropertyBag based on the provided key.
    /// If the specified key does not exist in the PropertyBag, the method will return the default value obtained from the defaultValue function.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="propertyBag">The PropertyBag to retrieve the value from.</param>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">The deserialized value.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>True if the value exists, false otherwise.</returns>
    public static bool TryGetValue<T>(this PropertyBag propertyBag, string key, out T value, JsonSerializerOptions? options = null)
    {
        if (!propertyBag.TryGetValue(key, out var v))
        {
            value = default!;
            return false;
        }

        var json = (string)v;
        value = JsonSerializer.Deserialize<T>(json, options);
        return true;
    }

    /// <summary>
    /// Retrieves a value from the PropertyBag based on the provided key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="propertyBag">The PropertyBag to retrieve the value from.</param>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The value associated with the key.</returns>
    public static T GetValue<T>(this PropertyBag propertyBag, string key, JsonSerializerOptions? options = null)
    {
        var json = propertyBag[key].ToString();
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Sets a value in the PropertyBag based on the provided key.
    /// The value is serialized using JSON.
    /// </summary>
    /// <param name="propertyBag">The PropertyBag to set the value in.</param>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store in the PropertyBag.</param>
    /// /// <param name="options">Optional JSON serializer options.</param>
    public static void SetValue(this PropertyBag propertyBag, string key, object value, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(value);
        propertyBag[key] = json;
    }
}