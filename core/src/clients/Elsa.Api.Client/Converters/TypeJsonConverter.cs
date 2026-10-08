using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Extensions;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Converters;

/// <summary>
/// Converts <see cref="Type"/> objects to and from their assembly-qualified name.
/// </summary>
[UsedImplicitly]
public class TypeJsonConverter : JsonConverter<Type>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Type) || typeToConvert.FullName == "System.RuntimeType";
    }

    /// <inheritdoc />
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString()!;

        // Handle collection types.
        if (typeName.EndsWith("[]"))
        {
            var elementTypeName = typeName[..^"[]".Length];
            var elementType = Type.GetType(elementTypeName)!;
            return typeof(List<>).MakeGenericType(elementType);
        }

        return Type.GetType(typeName);
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
                var elementTypeName = value.GetSimpleAssemblyQualifiedName();
                JsonSerializer.Serialize(writer, $"{elementTypeName}[]", options);
                return;
            }
        }

        var typeName = value.GetSimpleAssemblyQualifiedName();
        JsonSerializer.Serialize(writer, typeName, options);
    }
}