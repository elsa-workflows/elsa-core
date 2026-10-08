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
            return reader.GetInt32();

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString()!;
            return int.Parse(value, CultureInfo.InvariantCulture);
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