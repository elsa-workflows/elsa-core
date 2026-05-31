using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Input"/> objects.
/// </summary>
public class OutputJsonConverter<T> : JsonConverter<Output<T>?>
{
    private readonly ISerializationTypeRegistry _workflowJsonTypeRegistry;

    /// <inheritdoc />
    public OutputJsonConverter(ISerializationTypeRegistry workflowJsonTypeRegistry)
    {
        _workflowJsonTypeRegistry = workflowJsonTypeRegistry;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Output).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override Output<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return null;

        if (!doc.RootElement.TryGetProperty("typeName", out _))
            return null;

        var memoryReferenceElement = doc.RootElement.GetProperty("memoryReference");

        if (!memoryReferenceElement.TryGetProperty("id", out var memoryReferenceIdElement))
            return default;

        var variable = new Variable
        {
            Id = memoryReferenceIdElement.GetString()!
        };
        variable.Name = variable.Id;
        
        return (Output<T>)Activator.CreateInstance(typeof(Output<T>), variable)!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Output<T>? value, JsonSerializerOptions options)
    {
        var valueType = typeof(T);
        var valueTypeAlias = SerializationTypeResolver.TryGetAlias(_workflowJsonTypeRegistry, valueType, out var alias)
            ? alias
            : valueType.GetSimpleAssemblyQualifiedName();

        var model = new
        {
            TypeName = valueTypeAlias,
            MemoryReference = value == null ? null : new
            {
                Id = value.MemoryBlockReference().Id
            }
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}
