using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// A JSON converter for <see cref="PropertyBag"/> objects.
/// </summary>
public class PropertyBagConverter : JsonConverter<PropertyBag>
{
    /// <inheritdoc />
    public override PropertyBag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options)!;
        return new PropertyBag(dictionary);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PropertyBag value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Dictionary, options);
    }
}