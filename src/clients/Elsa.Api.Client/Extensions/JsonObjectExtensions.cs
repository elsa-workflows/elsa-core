using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonObject"/>.
/// </summary>
public static class JsonObjectExtensions
{
    /// <summary>
    /// Serializes the specified value to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>A <see cref="JsonObject"/> representing the specified value.</returns>
    public static JsonNode SerializeToNode(this object value, JsonSerializerOptions? options = default)
    {
        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.SerializeToNode(value, options)!;
    }
    
    /// <summary>
    /// Serializes the specified value to a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>A <see cref="JsonObject"/> representing the specified value.</returns>
    public static JsonArray SerializeToArray(this object value, JsonSerializerOptions? options = default)
    {
        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.SerializeToNode(value, options)!.AsArray();
    }
    
    /// <summary>
    /// Serializes the specified value to a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>A <see cref="JsonObject"/> representing the specified value.</returns>
    public static JsonArray SerializeToArray<T>(this IEnumerable<T> value, JsonSerializerOptions? options = default)
    {
        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.SerializeToNode(value, options)!.AsArray();
    }

    /// <summary>
    /// Deserializes the specified <see cref="JsonNode"/> to the specified type.
    /// </summary>
    /// <param name="value">The <see cref="JsonNode"/> to deserialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized value.</returns>
    public static T Deserialize<T>(this JsonNode value, JsonSerializerOptions? options = default)
    {
        options ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        if (value is JsonObject jsonObject)
            return JsonSerializer.Deserialize<T>(jsonObject, options)!;

        if (value is JsonArray jsonArray)
            return JsonSerializer.Deserialize<T>(jsonArray, options)!;

        if (value is JsonValue jsonValue)
            return jsonValue.GetValue<T>();

        throw new NotSupportedException($"Cannot deserialize {value.GetType()} to {typeof(T)}.");
    }

    /// <summary>
    /// Sets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to set the property value on.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the property.</param>
    public static void SetProperty(this JsonObject model, JsonNode? value, params string[] path)
    {
        model = GetPropertyContainer(model, path);
        model[path.Last()] = value?.SerializeToNode();
    }
    
    /// <summary>
    /// Sets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to set the property value on.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the property.</param>
    public static void SetProperty(this JsonObject model, JsonArray? value, params string[] path)
    {
        model = GetPropertyContainer(model, path);
        model[path.Last()] = value?.SerializeToNode();
    }
    
    /// <summary>
    /// Sets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to set the property value on.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the property.</param>
    public static void SetProperty(this JsonObject model, IEnumerable<JsonNode> value, params string[] path)
    {
        model = GetPropertyContainer(model, path);
        model[path.Last()] = new JsonArray(value.Select(x => x.SerializeToNode()).ToArray());
    }
    
    /// <summary>
    /// Gets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to get the property value from.</param>
    /// <param name="path">The path to the property.</param>
    /// <returns>The property value.</returns>
    public static JsonNode? GetProperty(this JsonObject model, params string[] path)
    {
        var currentModel = model;

        foreach (var prop in path.SkipLast(1))
        {
            if (currentModel[prop] is not JsonObject value)
                return default;

            currentModel = value;
        }

        return currentModel[path.Last()];
    }

    /// <summary>
    /// Gets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to get the property value from.</param>
    /// <param name="path">The path to the property.</param>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The property value.</returns>
    public static T? GetProperty<T>(this JsonObject model, params string[] path)
    {
        var property = GetProperty(model, path);
        return property != null ? property.Deserialize<T>() : default;
    }

    /// <summary>
    /// Gets the property value of the specified model.
    /// </summary>
    /// <param name="model">The model to get the property value from.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use when deserializing.</param>
    /// <param name="path">The path to the property.</param>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The property value.</returns>
    public static T? GetProperty<T>(this JsonObject model, JsonSerializerOptions options, params string[] path)
    {
        var property = GetProperty(model, path);
        return property != null ? property.Deserialize<T>(options) : default;
    }
    
    /// <summary>
    /// Returns the property container of the specified model.
    /// </summary>
    /// <param name="model">The model to set the property value on.</param>
    /// <param name="path">The path to the property.</param>
    private static JsonObject GetPropertyContainer(this JsonObject model, params string[] path)
    {
        foreach (var prop in path.SkipLast(1))
        {
            var property = model[prop] as JsonObject ?? new JsonObject();
            model[prop] = property;
            model = property;
        }

        return model;
    }

}