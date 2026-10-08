using System.Text.Json;

namespace Elsa.AI.Host.Services;

internal static class AIGroundingJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static JsonObject ToJsonObject<T>(T value) =>
        JsonSerializer.SerializeToNode(value, SerializerOptions) as JsonObject ?? [];

    public static JsonArray ToJsonArray<T>(IEnumerable<T> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(JsonSerializer.SerializeToNode(value, SerializerOptions));

        return array;
    }
}
