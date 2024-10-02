using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.ResourceManagement.Serialization.Converters;

public class ResourceConverter : JsonConverter<ResourceItem>
{
    public override ResourceItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ResourceItem value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}