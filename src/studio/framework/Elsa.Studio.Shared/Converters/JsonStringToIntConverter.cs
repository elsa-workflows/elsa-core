using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.Converters;

/// <summary>
/// Converts strings to and from integers.
/// </summary>
public class JsonStringToIntConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
            {
                var stringValue = reader.GetString();
                if (int.TryParse(stringValue, out var intValue))
                    return intValue;
                break;
            }
            case JsonTokenType.Number:
                return reader.GetInt32();
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to {nameof(Int32)}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}