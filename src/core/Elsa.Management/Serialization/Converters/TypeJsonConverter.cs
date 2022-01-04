using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Management.Contracts;

namespace Elsa.Management.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing said type.
/// </summary>
public class TypeJsonConverter : JsonConverter<Type>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public TypeJsonConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeAlias = reader.GetString()!;

        // Handle collection types.
        if (typeAlias.EndsWith("[]"))
        {
            var elementTypeAlias = typeAlias[..^"[]".Length];
            var elementType = _wellKnownTypeRegistry.TryGetType(typeAlias, out var t) ? t : Type.GetType(elementTypeAlias)!;
            return typeof(List<>).MakeGenericType(elementType);
        }

        return _wellKnownTypeRegistry.TryGetType(typeAlias, out var type) ? type : Type.GetType(typeAlias);
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        // Handle collection types.
        if (value.IsGenericType)
        {
            var elementType = value.GenericTypeArguments.First();
            var typedEnumerable = typeof(IEnumerable<>).MakeGenericType(elementType);

            if (typedEnumerable.IsAssignableFrom(value))
            {
                var elementTypeAlias = _wellKnownTypeRegistry.TryGetAlias(elementType, out var elementAlias) ? elementAlias : value.AssemblyQualifiedName;
                JsonSerializer.Serialize(writer, $"{elementTypeAlias}[]", options);
                return;
            }
        }

        var typeAlias = _wellKnownTypeRegistry.TryGetAlias(value, out var @alias) ? alias : value.AssemblyQualifiedName;
        JsonSerializer.Serialize(writer, typeAlias, options);
    }
}