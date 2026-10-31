using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// A JSON converter that safely serializes a value, even if it's not serializable.
/// In that case, the converter will serialize a fallback object that contains the type name of the original object.
/// </summary>
public class SafeValueConverter : JsonConverter<object>
{
    private JsonSerializerOptions? _options;
    
    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = GetClonedOptions(options);
        return JsonSerializer.Deserialize(ref reader, typeToConvert, newOptions)!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        try
        {
            var newOptions = GetClonedOptions(options);
            
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

    private JsonSerializerOptions GetClonedOptions(JsonSerializerOptions options)
    {
        if(_options != null)
            return _options;
        
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is SafeValueConverterFactory);
        _options = newOptions;
        return newOptions;
    }
}