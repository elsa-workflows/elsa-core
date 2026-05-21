using System.Reflection;
using System.Runtime.CompilerServices;
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
    private static readonly ConditionalWeakTable<Type, PropertyMetadata[]> PropertyCache = new();
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

        foreach (var metadata in GetSerializableProperties(value.GetType()))
        {
            var property = metadata.Property;
            var propertyValue = property.GetValue(value);

            if (ShouldIgnoreProperty(metadata.JsonIgnoreCondition, property.PropertyType, propertyValue))
                continue;

            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, newOptions);
        }

        writer.WriteEndObject();
    }

    private static PropertyMetadata[] GetSerializableProperties(Type type)
    {
        return PropertyCache.GetValue(type, static itemType => itemType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Select(property => new
            {
                Property = property,
                ExcludeFromHash = property.GetCustomAttribute<ExcludeFromHashAttribute>(),
                JsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>()
            })
            .Where(x => x.ExcludeFromHash == null && !ShouldAlwaysIgnoreProperty(x.JsonIgnore?.Condition))
            .Select(x => new PropertyMetadata(x.Property, x.JsonIgnore?.Condition))
            .ToArray());
    }

    private static bool ShouldAlwaysIgnoreProperty(JsonIgnoreCondition? condition)
    {
        return condition == JsonIgnoreCondition.Always;
    }

    private static bool ShouldIgnoreProperty(JsonIgnoreCondition? condition, Type declaredType, object? value)
    {
        return condition switch
        {
            null => false,
            JsonIgnoreCondition.Never => false,
            JsonIgnoreCondition.Always => true,
            JsonIgnoreCondition.WhenWritingNull => value == null,
            JsonIgnoreCondition.WhenWritingDefault => value == null || IsDefaultValue(declaredType, value),
            _ => false
        };
    }

    private static bool IsDefaultValue(Type declaredType, object value)
    {
        return declaredType.IsValueType && value.Equals(Activator.CreateInstance(declaredType));
    }

    private sealed record PropertyMetadata(PropertyInfo Property, JsonIgnoreCondition? JsonIgnoreCondition);
    
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
