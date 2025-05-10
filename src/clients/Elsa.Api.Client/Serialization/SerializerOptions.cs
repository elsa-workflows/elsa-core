using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Serialization;

/// <summary>
/// Provides options for serializing and deserializing objects.
/// </summary>
public static class SerializerOptions
{
    public static JsonSerializerOptions CommonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    /// <summary>
    /// Gets the serializer options for the log persistence configuration.
    /// </summary>
    public static JsonSerializerOptions LogPersistenceConfigSerializerOptions { get; } = new(CommonSerializerOptions);
    
    public static JsonSerializerOptions ResilienceStrategyConfigSerializerOptions { get; } = new(CommonSerializerOptions);
}