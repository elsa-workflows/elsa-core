using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing the type.
/// </summary>
[PublicAPI]
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
        var typeAlias = reader.GetString()!;

        // Handle collection types.
        if (typeAlias.EndsWith("[]"))
        {
            var elementTypeAlias = typeAlias[..^"[]".Length];
            var elementType = _wellKnownTypeRegistry.TryGetType(elementTypeAlias, out var t) ? t : Type.GetType(elementTypeAlias)!;
            return typeof(List<>).MakeGenericType(elementType);
        }

        return _wellKnownTypeRegistry.TryGetType(typeAlias, out var type) ? type : Type.GetType(typeAlias);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        // Handle collection types.
        if (value is { IsGenericType: true, GenericTypeArguments.Length: 1 })
        {
            var elementType = value.GenericTypeArguments.First();
            var typedEnumerable = typeof(IEnumerable<>).MakeGenericType(elementType);

            if (typedEnumerable.IsAssignableFrom(value))
            {
                var elementTypeAlias = _wellKnownTypeRegistry.TryGetAlias(elementType, out var elementAlias) ? elementAlias : value.GetSimpleAssemblyQualifiedName();
                JsonSerializer.Serialize(writer, $"{elementTypeAlias}[]", options);
                return;
            }
        }

        var typeAlias = _wellKnownTypeRegistry.TryGetAlias(value, out var @alias) ? alias : value.GetSimpleAssemblyQualifiedName();
        JsonSerializer.Serialize(writer, typeAlias, options);
    }
}