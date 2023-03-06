using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Serializes an object to JSON, excluding properties marked with <see cref="ExcludeFromHashAttribute"/>.
/// </summary>
public class ExcludeFromHashConverter : JsonConverter<object>
{
    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ExcludeFromHashConverterFactory);

        foreach (var property in value.GetType().GetProperties())
        {
            var attribute = property.GetCustomAttribute<ExcludeFromHashAttribute>();

            if (attribute != null)
            {
                continue;
            }

            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, property.GetValue(value), newOptions);
        }

        writer.WriteEndObject();
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