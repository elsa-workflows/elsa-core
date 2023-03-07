using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Ignores properties with the <see cref="JsonExpandableAttribute"/> attribute.
/// </summary>
public class JsonExpandableConverter<T> : JsonConverter<T>
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

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var newOptions = new JsonSerializerOptions(options);
        
        newOptions.Converters.RemoveWhere(x => x is JsonExpandableConverterFactory<T>);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonExpandableAttribute>() != null)
                continue;

            var propName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            writer.WritePropertyName(propName);
            JsonSerializer.Serialize(writer, property.GetValue(value), newOptions);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// A <see cref="JsonConverterFactory"/> that creates <see cref="JsonExpandableConverter{T}"/> instances.
/// </summary>
public class JsonExpandableConverterFactory<T> : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new JsonExpandableConverter<T>();
    }
}