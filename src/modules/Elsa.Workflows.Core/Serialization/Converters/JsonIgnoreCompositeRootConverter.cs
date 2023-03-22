using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Ignores properties with the <see cref="JsonIgnoreCompositeRootAttribute"/> attribute.
/// </summary>
public class JsonIgnoreCompositeRootConverter : JsonConverter<IActivity>
{
    /// <inheritdoc />
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var properties = value?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? Array.Empty<PropertyInfo>();
        var newOptions = new JsonSerializerOptions(options);
        
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;
            
            if (property.GetCustomAttribute<JsonIgnoreCompositeRootAttribute>() != null)
                continue;

            var propName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            writer.WritePropertyName(propName);
            JsonSerializer.Serialize(writer, property.GetValue(value), newOptions);
        }

        writer.WriteEndObject();
    }
}