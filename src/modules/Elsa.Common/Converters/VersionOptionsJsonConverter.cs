using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Models;

namespace Elsa.Common.Converters;

/// <summary>
/// A JSON converter that serializes <see cref="VersionOptions"/>.
/// </summary>
public class VersionOptionsJsonConverter : JsonConverter<VersionOptions>
{
    /// <inheritdoc />
    public override VersionOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var textValue = reader.GetString();
        return string.IsNullOrWhiteSpace(textValue) ? VersionOptions.Published : VersionOptions.FromString(textValue);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, VersionOptions value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}