using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.ResourceManagement.Serialization.Extensions;

public static class JsonNodeEx
{
    public static T? ToObject<T>(this JsonNode? jsonNode, JsonSerializerOptions? options = null) => jsonNode.Deserialize<T>(options ?? JsonSerializerOptions.Default);

    /// <summary>
    /// Creates an instance of the specified type from this <see cref="JsonNode"/>.
    /// </summary>
    public static object? ToObject(this JsonNode? jsonNode, Type type, JsonSerializerOptions? options = null) => jsonNode.Deserialize(type, options ?? JsonSerializerOptions.Default);
    
    /// <summary>
    /// Creates a <see cref="JsonNode"/> from an object.
    /// </summary>
    public static JsonNode? FromObject(object? obj, JsonSerializerOptions? options = null)
    {
        if (obj is JsonNode jsonNode)
        {
            return jsonNode;
        }

        if (obj is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Object => JsonObject.Create(jsonElement, JsonOptions.Node),
                JsonValueKind.Array => JsonArray.Create(jsonElement, JsonOptions.Node),
                _ => JsonValue.Create(jsonElement, JsonOptions.Node),
            };
        }

        return JsonSerializer.SerializeToNode(obj, options ?? JsonOptions.Default);
    }

    /// <summary>
    /// Gets the value of the specified type of this <see cref="JsonNode"/>.
    /// </summary>
    public static T? Value<T>(this JsonNode? jsonNode) => jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<T>(out var value) ? value : default;

    /// <summary>
    /// Gets the value of the specified type of this <see cref="JsonNode"/>.
    /// </summary>
    public static T? ValueOrDefault<T>(this JsonNode? jsonNode, T defaultValue) => jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<T>(out var value) ? value : defaultValue;

    /// <summary>
    /// Gets the value of the specified type from the specified property of this <see cref="JsonNode"/>.
    /// </summary>
    public static T? Value<T>(this JsonNode? jsonNode, string name) => jsonNode is not null ? jsonNode[name].Value<T>() : default;
}