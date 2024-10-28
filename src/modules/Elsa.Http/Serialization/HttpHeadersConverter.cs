using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Http.Serialization;

/// A custom JSON converter for HttpHeaders that supports both single and multiple values.
public class HttpHeadersConverter : JsonConverter<HttpHeaders>
{
    /// <inheritdoc />
    public override HttpHeaders Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        var headers = new HttpHeaders();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return headers;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a PropertyName token");

            var key = reader.GetString()!;
            reader.Read();

            // If the next token is not a StartArray token, then we expect a String token.
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                {
                    var values = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) values.Add(reader.GetString()!);
                    headers.Add(key, values.ToArray());
                    break;
                }
                case JsonTokenType.String:
                {
                    var singleValue = reader.GetString()!;
                    headers.Add(key, new[] { singleValue });
                    break;
                }
                default:
                    throw new JsonException("Expected a String or StartArray token");
            }
        }

        throw new JsonException("Expected an EndObject token");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, HttpHeaders value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var header in value)
        {
            writer.WritePropertyName(header.Key);
            writer.WriteStartArray();
            foreach (var headerValue in header.Value) writer.WriteStringValue(headerValue);
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}