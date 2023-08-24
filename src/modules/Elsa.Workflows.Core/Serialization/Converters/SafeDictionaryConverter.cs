using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter that safely serializes a dictionary of objects, even if some of the objects are not serializable.
/// In that case, the converter will serialize a fallback object that contains the type name of the original object.
/// </summary>
public class SafeDictionaryConverter : JsonConverter<IDictionary<string, object>>
{
    /// <inheritdoc />
    public override IDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options)!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IDictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            if (kvp.Value == null!) continue; // Skip null values

            writer.WritePropertyName(kvp.Key);

            try
            {
                // Serialize the value to a temporary string.
                var serializedValue = JsonSerializer.Serialize(kvp.Value, options);
            
                // Use the serialized string value to write to the main writer.
                using var doc = JsonDocument.Parse(serializedValue);
                doc.WriteTo(writer);
            }
            catch
            {
                // If serialization fails, write the fallback object.
                writer.WriteStartObject();
                writer.WriteString("TypeName", kvp.Value.GetType().FullName);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndObject();
    }
}