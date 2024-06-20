using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Helpers;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes the <see cref="ActivityNode"/> type and its descendant nodes based on the specified depth.
/// </summary>
/// <param name="depth">The number of levels of descendants to include. Defaults to 1.</param>
public class ActivityNodeConverter(ActivityWriter activityWriter, int depth = 1, int level = 0) : JsonConverter<ActivityNode>
{
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ActivityNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("nodeId", value.NodeId);
        writer.WriteString("port", value.Port);
        writer.WritePropertyName("activity");
        activityWriter.WriteActivity(writer, value.Activity, options, excludeChildren: true);

        if (level < depth)
        {
            writer.WritePropertyName("children");
            writer.WriteStartArray();

            var nodeConverter = new ActivityNodeConverter(activityWriter, depth, 1);
            var newOptions = options.Clone();
            newOptions.Converters.Remove(this);
            newOptions.Converters.Add(nodeConverter);

            foreach (var child in value.Children)
                JsonSerializer.Serialize(writer, child, newOptions);

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override ActivityNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}