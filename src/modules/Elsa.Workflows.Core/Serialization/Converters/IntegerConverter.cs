using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Converts integers to and from JSON strings.
/// </summary>
public class IntegerConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the JSON string value and parse it as an integer
        var value = reader.GetString()!;
        var integer = int.Parse(value);
        
        // Return the parsed integer
        return integer;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Write the integer as a JSON number
        writer.WriteNumberValue(value);
    }
}