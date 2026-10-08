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
            return reader.GetInt64();

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString()!;
            return long.Parse(value);
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