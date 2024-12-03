using System.Text.Json;

namespace Elsa.Common.Serialization;

public static class SerializerOptions
{
    public static JsonSerializerOptions ConfigurationJsonSerializerOptions { get; } = new()
    {
        Converters = { new ConfigurationJsonConverter() }
    };
}