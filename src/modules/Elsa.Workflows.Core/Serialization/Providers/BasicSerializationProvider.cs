using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Expressions.Services;
using Elsa.Extensions;
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
    public bool Supports(Type type) => true;

    /// <inheritdoc />
    public string Serialize(object? value)
    {
        if (CanSerialize(value, out var jsonElement))
            return jsonElement ?? string.Empty;

        var unserializableTypeHolder = new JsonObject
        {
            ["UnserializableType"] = value!.GetType().GetSimpleAssemblyQualifiedName()
        };
        
        var serializedTypeHolder = JsonSerializer.Serialize(unserializableTypeHolder);
        return serializedTypeHolder;
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    private static bool CanSerialize(object? obj, out string? jsonElement)
    {
        if (obj == null)
        {
            jsonElement = default;
            return true;
        }

        var options = CreateOptions();

        try
        {
            jsonElement = JsonSerializer.Serialize(obj, options);
            return true;
        }
        catch (Exception)
        {
            jsonElement = default;
            return false;
        }
    }

    private static JsonSerializerOptions CreateOptions() => new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()),
            new SafeDictionaryConverterFactory()
        }
    };
}