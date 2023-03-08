using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Converts primitives to and from JSON strings.
/// </summary>
public class JsonPrimitiveToStringConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return "True";
            case JsonTokenType.False:
                return "False";
            case JsonTokenType.Number when reader.TryGetInt64(out var l):
                return l.ToString();
            case JsonTokenType.Number:
                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.String when reader.TryGetDateTimeOffset(out var datetime):
                return datetime.ToString();
            case JsonTokenType.String:
                return reader.GetString()!;
            default:
            {
                // Use JsonElement as fallback.
                using var document = JsonDocument.ParseValue(ref reader);
                return document.RootElement.Clone().ToString();
            }
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}