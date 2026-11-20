using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Common.Converters;

/// <summary>
/// Converts big integers to and from JSON strings.
/// </summary>
public class BigIntegerJsonConverter : JsonConverter<BigInteger>
{
    /// <inheritdoc />
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.TryGetInt64(out var number) ? number : throw new JsonException("The JSON number is not a valid 64-bit integer.");

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : throw new JsonException($"'{value}' is not a valid 64-bit integer.");
        }

        throw new JsonException("Expected number or string.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        // Write the Big Integer as a JSON number.
        writer.WriteNumberValue((long)value);
    }
}