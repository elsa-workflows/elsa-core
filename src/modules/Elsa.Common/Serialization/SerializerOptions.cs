using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Common.Serialization;

public static class SerializerOptions
{
    public static JsonSerializerOptions CommonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public static JsonSerializerOptions ConfigurationJsonSerializerOptions { get; } = new()
    {
        Converters = { new ConfigurationJsonConverter() }
    };
    
    /// <summary>
    /// Gets the serializer options for the log persistence configuration.
    /// </summary>
    public static JsonSerializerOptions LogPersistenceConfigSerializerOptions { get; } = new(CommonSerializerOptions);
    
    public static JsonSerializerOptions ResilienceStrategyConfigSerializerOptions { get; } = new(CommonSerializerOptions);
}