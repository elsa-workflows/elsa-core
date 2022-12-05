using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Used for reading objects as primitive types rather than <see cref="JsonElement"/> values.
/// </summary>
public class SystemObjectPrimitiveConverter : JsonConverter<object>
{
    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number when reader.TryGetInt64(out var l):
                return l;
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.String when reader.TryGetDateTimeOffset(out var datetime):
                return datetime;
            case JsonTokenType.String:
                return reader.GetString();
            default:
            {
                // Use JsonElement as fallback.
                using var document = JsonDocument.ParseValue(ref reader);
                return document.RootElement.Clone();
            }
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new InvalidOperationException("Should not get here.");
    }
}