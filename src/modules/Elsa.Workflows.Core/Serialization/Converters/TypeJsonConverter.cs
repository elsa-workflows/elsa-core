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
        if (_wellKnownTypeRegistry.TryGetAlias(value, out var alias))
        {
            writer.WriteStringValue(alias);
            return;
        }

        // Handle array types.
        if (value.IsArray)
        {
            var elementType = value.GetElementType()!;
            var elementTypeAlias = _wellKnownTypeRegistry.TryGetAlias(elementType, out var elementTypeAliasValue) ? elementTypeAliasValue : elementType.GetSimpleAssemblyQualifiedName();
            writer.WriteStringValue($"{elementTypeAlias}[]");
            return;
        }
        
        // Handle collection types.
        if (value is { IsGenericType: true, GenericTypeArguments.Length: 1 })
        {
            var elementType = value.GenericTypeArguments.First();
            var typedEnumerable = typeof(IEnumerable<>).MakeGenericType(elementType);

            if (typedEnumerable.IsAssignableFrom(value) && _wellKnownTypeRegistry.TryGetAlias(elementType, out var elementTypeAlias))
            {
                writer.WriteStringValue($"List<{elementTypeAlias}>");
                return;
            }
        }

        writer.WriteStringValue(value.GetSimpleAssemblyQualifiedName());
    }
}
