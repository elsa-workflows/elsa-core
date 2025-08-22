using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Logging.Serialization;

public class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle real boolean
        if (reader.TokenType == JsonTokenType.True)
            return true;
        if (reader.TokenType == JsonTokenType.False)
            return false;
        // Handle string "true"/"false"
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (bool.TryParse(value, out var b))
                return b;
        }

        // Handle null
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        throw new JsonException($"Cannot convert {reader.TokenType} to bool?");
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }
}