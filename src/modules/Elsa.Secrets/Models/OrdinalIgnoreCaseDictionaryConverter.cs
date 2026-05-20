using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Secrets.Models;

public class OrdinalIgnoreCaseDictionaryConverter : JsonConverter<IDictionary<string, string>>
{
    public override IDictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected an object of string metadata values.");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return values;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a metadata property name.");

            var key = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON while reading secret metadata.");

            if (reader.TokenType == JsonTokenType.Null)
                continue;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected a string metadata value.");

            var value = reader.GetString();
            if (value != null)
                values[key] = value;
        }

        throw new JsonException("Unexpected end of JSON while reading secret metadata.");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var item in value.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            writer.WriteString(item.Key, item.Value);
        writer.WriteEndObject();
    }
}
