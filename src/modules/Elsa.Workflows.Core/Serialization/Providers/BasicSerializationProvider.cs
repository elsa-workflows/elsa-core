using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Serialization.Providers;

/// <summary>
/// Provides basic serialization for a given value.
/// This provider cannot serialize streams or other complex, non-serializable types. If such an attempt is made, only the type name will be serialized.
/// To serialize such types, implement <see cref="ISerializationProvider"/> and register it with the service container.
/// </summary>
public class BasicSerializationProvider : ISerializationProvider
{
    /// <inheritdoc />
    public float Priority { get; set; } = -1;

    /// <inheritdoc />
    public bool Supports(object? value) => true;

    /// <inheritdoc />
    public ValueTask<JsonElement> SerializeAsync(object? value, CancellationToken cancellationToken = default)
    {
        if (CanSerialize(value, out var jsonElement))
            return new(jsonElement);

        var type = JsonSerializer.SerializeToElement(value!.GetType().Name);
        return new(type);
    }

    private static bool CanSerialize(object? obj, out JsonElement jsonElement)
    {
        if (obj == null)
        {
            jsonElement = default;
            return true;
        }

        var options = CreateOptions();

        try
        {
            jsonElement = JsonSerializer.SerializeToElement(obj, options);
            return true;
        }
        catch (JsonException)
        {
            jsonElement = default;
            return false;
        }
    }
    
    private static JsonSerializerOptions CreateOptions() => new()
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault())
        }
    };
}