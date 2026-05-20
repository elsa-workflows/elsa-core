using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Serialization.Helpers;
using JetBrains.Annotations;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <inheritdoc />
    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Type) || typeToConvert.FullName == "System.RuntimeType";
    }

    /// <inheritdoc />
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeAlias = reader.GetString();
        return WorkflowJsonTypeResolver.ResolveType(_wellKnownTypeRegistry, typeAlias);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (!WorkflowJsonTypeResolver.TryGetAlias(_wellKnownTypeRegistry, value, out var typeAlias))
            throw new JsonException($"Type '{value}' is not registered in the workflow JSON type registry.");

        writer.WriteStringValue(typeAlias);
    }
}
