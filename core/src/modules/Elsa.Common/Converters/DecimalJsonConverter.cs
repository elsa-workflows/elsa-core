using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Common.Converters;

/// <summary>
/// Converts decimals to and from JSON strings.
/// </summary>
public class DecimalJsonConverter : JsonConverter<decimal>
{
    /// <inheritdoc />
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.TryGetDecimal(out var number) ? number : throw new JsonException("The JSON number is not a valid decimal.");

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : throw new JsonException($"'{value}' is not a valid decimal.");
        }

        throw new JsonException("Expected number or string.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // Write the decimal as a JSON number
        writer.WriteNumberValue(value);
    }
}