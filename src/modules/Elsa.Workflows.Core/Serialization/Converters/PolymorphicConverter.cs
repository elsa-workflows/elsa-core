using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Extensions;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A converter that stores type information in order to deserialize the object back into the same type. 
/// </summary>
public class PolymorphicConverter : JsonConverter<object>
{
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var typeName = value.GetType().GetSimpleAssemblyQualifiedName();
        var wrappedValue = JsonSerializer.SerializeToNode(value, options)!;
        wrappedValue["$type"] = typeName;
        wrappedValue.WriteTo(writer);
    }

    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var typeName = element.GetProperty("$type").GetString()!;
        var type = Type.GetType(typeName)!;
        var value = element.Deserialize(type, options);

        return value!;
    }
}