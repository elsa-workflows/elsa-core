using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Common.Converters;

public class BooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.String:
                var value = reader.GetString();
                if (bool.TryParse(value, out var b))
                    return b;
                break;
        }
        throw new JsonException($"Cannot convert {reader.TokenType} to bool");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}