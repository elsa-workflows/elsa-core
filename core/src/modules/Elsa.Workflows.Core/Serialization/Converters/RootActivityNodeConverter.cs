using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Helpers;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes the <see cref="ActivityNode"/> type without its children. Instead, it will serialize the activity children as properties.
/// </summary>
public class RootActivityNodeConverter(ActivityWriter activityWriter, bool excludeChildren = false) : JsonConverter<ActivityNode>
{
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ActivityNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("nodeId", value.NodeId);
        writer.WriteString("port", value.Port);
        writer.WritePropertyName("activity");
        activityWriter.WriteActivity(writer, value.Activity, options, excludeChildren: excludeChildren);
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override ActivityNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}