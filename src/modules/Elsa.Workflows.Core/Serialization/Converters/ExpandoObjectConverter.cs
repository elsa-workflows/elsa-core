using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Parses a JON string into a dynamic <see cref="ExpandoObject"/>.
/// </summary>
public sealed class ExpandoObjectConverter : JsonConverter<object>
{
    private readonly JsonConverter<object> _objectConverter;
    private const string IdPropertyName = "$id";

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
    /// </summary>
    public ExpandoObjectConverter()
    {
        _objectConverter = (JsonConverter<object>)new PolymorphicObjectConverterFactory().CreateConverter(typeof(object), new JsonSerializerOptions());
    }
    
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, typeof(ExpandoObject), options);
    }

    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null!;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.String:
                return reader.GetString()!;
            case JsonTokenType.Number:
            {
                if (reader.TryGetInt32(out var i))
                    return i;
                if (reader.TryGetInt64(out var l))
                    return l;
                // BigInteger could be added here.

                if (reader.TryGetDouble(out var d))
                    return d;
                using var doc = JsonDocument.ParseValue(ref reader);
                return doc.RootElement.Clone();
            }
            case JsonTokenType.StartArray:
            {
                var list = new List<object>();
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        default:
                            list.Add(Read(ref reader, typeof(object), options));
                            break;
                        case JsonTokenType.EndArray:
                            return list;
                    }
                }

                throw new JsonException();
            }
            case JsonTokenType.StartObject:
                var dict = CreateDictionary();
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.EndObject:
                            return dict;
                        case JsonTokenType.PropertyName:
                            var key = reader.GetString()!;
                            reader.Read();
                            if (key != IdPropertyName)
                            {
                                var value = _objectConverter.Read(ref reader, typeof(object), options)!;
                                dict.Add(key, value);
                            }
                            break;
                        default:
                            throw new JsonException();
                    }
                }

                throw new JsonException();
            default:
                throw new JsonException($"Unknown token {reader.TokenType}");
        }
    }

    private IDictionary<string, object> CreateDictionary() => new ExpandoObject()!;
}