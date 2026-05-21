using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Secrets.Models;

public class CaseInsensitiveHashSetConverter : JsonConverter<HashSet<string>>
{
    public override HashSet<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected an array of strings.");

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected a string tag.");

            var value = reader.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }

        throw new JsonException("Unexpected end of JSON while reading secret tags.");
    }

    public override void Write(Utf8JsonWriter writer, HashSet<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Order(StringComparer.OrdinalIgnoreCase))
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}
