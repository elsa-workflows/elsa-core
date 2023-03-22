using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

public class PolymorphicDictionaryConverter : JsonConverter<IDictionary<string, object>>
{
    private readonly JsonConverter<object> _objectConverter;

    /// <inheritdoc />
    public PolymorphicDictionaryConverter(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Insert(0, new SystemObjectWithTypeHandlingConverterFactory());
        _objectConverter = (JsonConverter<object>)newOptions.GetConverter(typeof(object));
    }

    /// <inheritdoc />
    public override IDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object.");
        }

        var dictionary = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            var key = reader.GetString()!;
            reader.Read();
            var value = _objectConverter.Read(ref reader, typeof(object), options)!;
            dictionary.Add(key, value);
        }

        throw new JsonException("Expected end of object.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IDictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            _objectConverter.Write(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}