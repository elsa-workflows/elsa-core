using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Logging.Serialization;

/// <summary>
/// A custom JSON converter for nullable boolean values.
/// Provides functionality to serialize and deserialize nullable boolean values
/// in JSON, including support for the string representation of boolean values
/// ("true", "false") and handling of null cases.
/// </summary>
public class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // Handle real boolean
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            // Handle string "true"/"false"
            case JsonTokenType.String:
                {
                    var value = reader.GetString();
                    if (bool.TryParse(value, out var b))
                        return b;
                    break;
                }
        }

        // Handle null
        return reader.TokenType == JsonTokenType.Null ? null : throw new JsonException($"Cannot convert {reader.TokenType} to bool?");
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }
}