using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Common.Converters;

/// <summary>
/// Converts integers to and from JSON strings.
/// </summary>
public class IntegerJsonConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.TryGetInt32(out var number) ? number : throw new JsonException("The JSON number is not a valid integer.");

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : throw new JsonException($"'{value}' is not a valid integer.");
        }

        throw new JsonException("Expected number or string.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Write the integer as a JSON number.
        writer.WriteNumberValue(value);
    }
}