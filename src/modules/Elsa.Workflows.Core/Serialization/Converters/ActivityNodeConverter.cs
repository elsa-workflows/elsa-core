using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes the <see cref="ActivityNode"/> type and its immediate child nodes.
/// </summary>
/// <param name="depth">The level of descendants to include. Defaults to 1.</param>
public class ActivityNodeConverter(int depth = 1, int level = 0) : JsonConverter<ActivityNode>
{
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ActivityNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("nodeId", value.NodeId);
        writer.WriteString("port", value.Port);
        writer.WritePropertyName("activity");
        WriteActivity(writer, value.Activity, options);

        if (level < depth)
        {
            writer.WritePropertyName("children");
            writer.WriteStartArray();

            var nodeConverter = new ActivityNodeConverter(depth, 1);
            var newOptions = options.Clone();
            newOptions.Converters.Remove(this);
            newOptions.Converters.Add(nodeConverter);

            foreach (var child in value.Children)
            {
                JsonSerializer.Serialize(writer, child, newOptions);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override ActivityNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private void WriteActivity(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        // Check if there's a specialized converter for the activity.
        var valueType = value.GetType();
        var specializedConverter = options.Converters.FirstOrDefault(x => x.CanConvert(valueType));
        if (specializedConverter != null)
        {
            JsonSerializer.Serialize(writer, value, valueType, options);
            return;
        }
        
        writer.WriteStartObject();

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            if (typeof(IActivity).IsAssignableFrom(property.PropertyType))
                continue;

            if (typeof(IEnumerable<IActivity>).IsAssignableFrom(property.PropertyType))
                continue;

            var propName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            writer.WritePropertyName(propName);
            var input = property.GetValue(value);

            if (input == null)
            {
                writer.WriteNullValue();
                continue;
            }

            if (property.Name == nameof(IActivity.CustomProperties))
            {
                var customProperties = new Dictionary<string, object>(value.CustomProperties);
                foreach (var kvp in customProperties)
                {
                    if (kvp.Value is IActivity or IEnumerable<IActivity>)
                        customProperties.Remove(kvp.Key);
                }

                input = customProperties;
            }

            JsonSerializer.Serialize(writer, input, options);
        }

        writer.WriteEndObject();
    }
}