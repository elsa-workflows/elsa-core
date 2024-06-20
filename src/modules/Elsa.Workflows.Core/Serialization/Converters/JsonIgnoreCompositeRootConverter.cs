using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Serialization.Helpers;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Ignores properties with the <see cref="JsonIgnoreCompositeRootAttribute"/> attribute.
/// </summary>
public class JsonIgnoreCompositeRootConverter(IActivityRegistry activityRegistry, IExpressionDescriptorRegistry expressionDescriptorRegistry) : JsonConverter<IActivity>
{
    /// <inheritdoc />
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override void Write(Utf8JsonWriter writer, IActivity? value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value != null)
        {
            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                if (property.GetCustomAttribute<JsonIgnoreCompositeRootAttribute>() != null)
                    continue;

                var propName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                writer.WritePropertyName(propName);
                var input = property.GetValue(value);

                if (input == null)
                {
                    writer.WriteNullValue();
                    continue;
                }

                JsonSerializer.Serialize(writer, input, options);
            }

            var activityDescriptor = activityRegistry.Find(value.Type, value.Version);
            if (activityDescriptor != null)
                SyntheticPropertiesWriter.WriteSyntheticProperties(writer, value, activityDescriptor, expressionDescriptorRegistry, options);
        }

        writer.WriteEndObject();
    }
}