using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Serialization.Converters;

public class PropertyBagConverter : JsonConverter<PropertyBag>
{
    public override PropertyBag? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader);
        return new PropertyBag(dictionary);
    }

    public override void Write(Utf8JsonWriter writer, PropertyBag value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Properties);
    }
}