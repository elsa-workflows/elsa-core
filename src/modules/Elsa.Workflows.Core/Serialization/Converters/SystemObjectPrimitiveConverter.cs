using System.Dynamic;
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
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTimeOffset(out var datetime) => datetime,
            JsonTokenType.String => reader.GetString(),
            _ => JsonSerializer.Deserialize(ref reader, typeof(ExpandoObject), options)
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType());
    }
}