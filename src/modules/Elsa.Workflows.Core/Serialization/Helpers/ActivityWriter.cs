using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Helpers;

/// <summary>
/// The ActivityWriter class is responsible for writing an activity to a JSON writer using the provided options.
/// </summary>
public class ActivityWriter(IActivityRegistry activityRegistry, SyntheticPropertiesWriter syntheticPropertiesWriter, ILogger<ActivityWriter> logger)
{
    /// <summary>
    /// Writes an activity to a JSON writer using the provided options.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The activity to write.</param>
    /// <param name="options">The JSON serialization options.</param>
    /// <param name="ignoreSpecializedConverters">Controls whether to ignore the availability of converters that can handle IActivity objects.</param>
    /// <param name="excludeChildren">A flag indicating whether to exclude child activities.</param>
    /// <param name="propertyFilter">An additional property filter. Returning true will skip the property.</param>
    public void WriteActivity(Utf8JsonWriter writer, IActivity? value, JsonSerializerOptions options, bool ignoreSpecializedConverters = false, bool excludeChildren = false, Func<PropertyInfo, bool>? propertyFilter = null)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (!ignoreSpecializedConverters)
        {
            // Check if there's a specialized converter for the activity.
            var valueType = value.GetType();
            var specializedConverter = options.Converters.FirstOrDefault(x => x.CanConvert(valueType));
            if (specializedConverter != null)
            {
                JsonSerializer.Serialize(writer, value, valueType, options);
                return;
            }
        }

        writer.WriteStartObject();

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            if (excludeChildren)
            {
                if (typeof(IActivity).IsAssignableFrom(property.PropertyType))
                    continue;

                if (typeof(IEnumerable<IActivity>).IsAssignableFrom(property.PropertyType))
                    continue;
            }

            if (propertyFilter?.Invoke(property) == true)
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

        var activityDescriptor = activityRegistry.Find(value.Type, value.Version);

        if (activityDescriptor == null)
            logger.LogDebug("No descriptor found for activity {ActivityType}", value.GetType().Name);
        else
            syntheticPropertiesWriter.WriteSyntheticProperties(writer, value, activityDescriptor, options);

        writer.WriteEndObject();
    }
}