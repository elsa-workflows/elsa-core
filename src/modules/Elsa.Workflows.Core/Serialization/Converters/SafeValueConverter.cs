using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter that safely serializes a value, even if it's not serializable.
/// In that case, the converter will serialize a fallback object that contains the type name of the original object.
/// </summary>
public class SafeValueConverter : JsonConverter<object>
{
    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = CreateNewOptions(options);
        return JsonSerializer.Deserialize(ref reader, typeToConvert, newOptions)!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        try
        {
            var newOptions = CreateNewOptions(options);
            
            // Serialize the value to a temporary string.
            var serializedValue = JsonSerializer.Serialize(value, newOptions);

            // Use the serialized string value to write to the main writer.
            using var doc = JsonDocument.Parse(serializedValue);
            doc.WriteTo(writer);
        }
        catch
        {
            // If serialization fails, write the fallback object.
            writer.WriteStartObject();
            writer.WriteString("TypeName", value.GetType().FullName);
            writer.WriteEndObject();
        }
    }

    private JsonSerializerOptions CreateNewOptions(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is SafeValueConverterFactory);
        return newOptions;
    }
}