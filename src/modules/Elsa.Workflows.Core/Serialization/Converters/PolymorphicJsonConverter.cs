using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;

namespace Elsa.Workflows.Core.Serialization.Converters;

public class PolymorphicJsonConverter : JsonConverter<object>
{
    private const string TypePropertyName = "_type";

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDocument = JsonDocument.ParseValue(ref reader))
        {
            var jsonElement = jsonDocument.RootElement;
            var deserializedObject = DeserializeExpandoObject(jsonElement, options);
            return deserializedObject;
        }
    }


    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is IDictionary<string, object> dictionary)
        {
            writer.WriteStartObject();

            foreach (var keyValuePair in dictionary)
            {
                writer.WritePropertyName(keyValuePair.Key);

                if (keyValuePair.Value is ExpandoObject expandoValue)
                {
                    Write(writer, expandoValue, options);
                }
                else
                {
                    WriteWithType(writer, keyValuePair.Value, options);
                }
            }

            writer.WriteEndObject();
        }
        else if (value is IEnumerable enumerable && !(value is string))
        {
            writer.WriteStartArray();

            foreach (var item in enumerable)
            {
                if (item is ExpandoObject expandoItem)
                {
                    Write(writer, expandoItem, options);
                }
                else
                {
                    WriteWithType(writer, item, options);
                }
            }

            writer.WriteEndArray();
        }
        else
        {
            WriteWithType(writer, value, options);
        }
    }



    private void WriteWithType(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        Type valueType = value.GetType();

        if (!valueType.IsPrimitive && valueType != typeof(string) && valueType != typeof(TimeSpan) && valueType != typeof(DateTime) && !(value is ExpandoObject) && !(value is IDictionary) && !(value is IEnumerable) && valueType.IsClass)
        {
            writer.WriteStartObject();
            writer.WriteString(TypePropertyName, $"{valueType.FullName}, {valueType.Assembly.GetName().Name}");

            WriteObjectProperties(writer, value, options);

            writer.WriteEndObject();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    
    private void WriteObjectProperties(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        Type valueType = value.GetType();
        foreach (var property in valueType.GetProperties())
        {
            if (property.CanRead)
            {
                writer.WritePropertyName(property.Name);
                WriteWithType(writer, property.GetValue(value), options);
            }
        }
    }



    private object DeserializeExpandoObject(JsonElement jsonElement, JsonSerializerOptions options)
    {
        if (jsonElement.TryGetProperty(TypePropertyName, out var typeProperty))
        {
            var targetType = Type.GetType(typeProperty.GetString());
            var targetObject = JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType, options);
            return targetObject;
        }

        var expandoObject = new ExpandoObject() as IDictionary<string, object>;

        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.Name == "$id")
            {
                continue;
            }

            object deserializedValue;
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    deserializedValue = DeserializeExpandoObject(property.Value, options);
                }
                    break;
                case JsonValueKind.String:
                    deserializedValue = property.Value.GetString();
                    break;
                case JsonValueKind.Number:
                    if (property.Value.TryGetInt32(out int intValue))
                        deserializedValue = intValue;
                    else if (property.Value.TryGetInt64(out long longValue))
                        deserializedValue = longValue;
                    else
                        deserializedValue = property.Value.GetDouble();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    deserializedValue = property.Value.GetBoolean();
                    break;
                case JsonValueKind.Null:
                    deserializedValue = null;
                    break;
                default:
                    deserializedValue = JsonSerializer.Deserialize<object>(property.Value.GetRawText(), options);
                    break;
            }

            expandoObject.Add(property.Name, deserializedValue);
        }

        return expandoObject;
    }
}