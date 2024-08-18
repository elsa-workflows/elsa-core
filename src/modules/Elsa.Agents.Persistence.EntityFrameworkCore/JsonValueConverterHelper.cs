using System.Text.Json;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

internal static class JsonValueConverterHelper
{
    public static T Deserialize<T>(string json) where T : class
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json);
    }

    public static string Serialize<T>(T obj) where T : class
    {
        return obj == null ? null : JsonSerializer.Serialize(obj);
    }
}