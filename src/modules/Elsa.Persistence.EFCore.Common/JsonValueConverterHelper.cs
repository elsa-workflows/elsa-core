using System.Text.Json;
using Elsa.Persistence.EFCore.Converters;
using Elsa.Extensions;

namespace Elsa.Persistence.EFCore;

internal static class JsonValueConverterHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    public static T Deserialize<T>(string json) where T : class
    {
        return (string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, JsonSerializerOptions))!;
    }

    public static string Serialize<T>(T obj) where T : class
    {
        return (obj == null ? null : JsonSerializer.Serialize(obj, JsonSerializerOptions))!;
    }
    
    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions().WithConverters(new PrimitiveDictionaryConverter());
    }
}