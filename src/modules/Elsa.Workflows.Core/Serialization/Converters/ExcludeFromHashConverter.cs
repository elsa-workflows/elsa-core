using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes an object to JSON, excluding properties marked with <see cref="ExcludeFromHashAttribute"/>.
/// </summary>
public class ExcludeFromHashConverter : JsonConverter<object>
{
    private JsonSerializerOptions? _options;
    
    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var newOptions = GetClonedOptions(options);

        foreach (var property in value.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            var attribute = property.GetCustomAttribute<ExcludeFromHashAttribute>();
            var jsonIgnoreAttribute = property.GetCustomAttribute<JsonIgnoreAttribute>();

            if (attribute != null || ShouldAlwaysIgnoreProperty(jsonIgnoreAttribute))
            {
                continue;
            }

            var propertyValue = property.GetValue(value);

            if (ShouldIgnoreProperty(jsonIgnoreAttribute, property.PropertyType, propertyValue))
                continue;

            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, newOptions);
        }

        writer.WriteEndObject();
    }

    private static bool ShouldAlwaysIgnoreProperty(JsonIgnoreAttribute? attribute)
    {
        return attribute?.Condition == JsonIgnoreCondition.Always;
    }

    private static bool ShouldIgnoreProperty(JsonIgnoreAttribute? attribute, Type declaredType, object? value)
    {
        return attribute?.Condition switch
        {
            null => false,
            JsonIgnoreCondition.Never => false,
            JsonIgnoreCondition.Always => true,
            JsonIgnoreCondition.WhenWritingNull => value == null,
            JsonIgnoreCondition.WhenWritingDefault => value == null || IsDefaultValue(declaredType, value),
            _ => throw new JsonException($"Unsupported JSON ignore condition '{attribute.Condition}'.")
        };
    }

    private static bool IsDefaultValue(Type declaredType, object value)
    {
        return declaredType.IsValueType && value.Equals(Activator.CreateInstance(declaredType));
    }
    
    private JsonSerializerOptions GetClonedOptions(JsonSerializerOptions options)
    {
        if(_options != null)
            return _options;
        
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ExcludeFromHashConverterFactory);
        return _options = newOptions;
    }
}

/// <summary>
/// A factory for creating <see cref="ExcludeFromHashConverter"/> instances.
/// </summary>
public class ExcludeFromHashConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => true;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ExcludeFromHashConverter();
}
