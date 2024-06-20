using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

public record IgnoreChildActivitiesMarker;
public class IgnoreChildActivitiesConverter : JsonConverter<IgnoreChildActivitiesMarker>
{
    public override IgnoreChildActivitiesMarker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
    public override void Write(Utf8JsonWriter writer, IgnoreChildActivitiesMarker value, JsonSerializerOptions options) => throw new NotImplementedException();
}