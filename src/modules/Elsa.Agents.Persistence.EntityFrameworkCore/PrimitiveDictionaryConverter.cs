using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

public class PrimitiveDictionaryConverter : JsonConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object.");

        var dictionary = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary;

            var key = reader.GetString()!;
            reader.Read();
            var value = ReadValue(ref reader, options);
            dictionary.Add(key, value);
        }

        throw new JsonException("Expected end of object.");
    }

    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString()!;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var l))
                    return l;
                return reader.GetDouble();
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null!;
            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options)!;
            case JsonTokenType.StartArray:
                return JsonSerializer.Deserialize<List<object>>(ref reader, options)!;
            default:
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.Clone().ToString();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}