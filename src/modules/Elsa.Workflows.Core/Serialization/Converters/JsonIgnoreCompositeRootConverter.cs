using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Ignores properties with the <see cref="JsonIgnoreCompositeRootAttribute"/> attribute.
/// </summary>
public class JsonIgnoreCompositeRootConverter<T> : JsonConverter<T>
{
    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
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

/// <summary>
/// A <see cref="JsonConverterFactory"/> that creates <see cref="JsonIgnoreCompositeRootConverter{T}"/> instances.
/// </summary>
public class JsonIgnoreCompositeRootConverterFactory<T> : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new JsonIgnoreCompositeRootConverter<T>();
    }
}