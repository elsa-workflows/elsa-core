using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Aspects.Serialization;

namespace Elsa.Aspects;

public static class JsonNodeExtensions
{
    public static T ToObject<T>(this JsonNode? jsonNode, JsonSerializerOptions? options = null)
    {
        return jsonNode.Deserialize<T>(options ?? SerializerOptions.Default)!;
    }

    public static object ToObject(this JsonNode? jsonNode, Type type, JsonSerializerOptions? options = null)
    {
        return jsonNode.Deserialize(type, options ?? SerializerOptions.Default)!;
    }
}