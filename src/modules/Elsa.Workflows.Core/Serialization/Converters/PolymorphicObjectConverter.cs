using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Used for reading objects as primitive types rather than <see cref="JsonElement"/> values while also maintaining the .NET type name for reconstructing the actual type.
/// </summary>
public class PolymorphicObjectConverter : JsonConverter<object>
{
    private const string TypePropertyName = "_type";
    private const string ItemsPropertyName = "_items";

    /// <inheritdoc />
    public PolymorphicObjectConverter()
    {
    }

    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return ReadPrimitive(ref reader, newOptions);
        }

        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var jsonObject = jsonDocument.RootElement;

        if (!jsonObject.TryGetProperty(TypePropertyName, out var typeNameElement))
        {
            newOptions.Converters.RemoveWhere(x => x is PolymorphicObjectConverterFactory);
            return jsonObject.Deserialize(typeof(ExpandoObject), newOptions)!;
        }

        var typeName = typeNameElement.GetString()!;
        var targetType = Type.GetType(typeName);

        if (targetType == null)
        {
            return JsonSerializer.Deserialize(ref reader, typeof(ExpandoObject), newOptions)!;
        }
        
        if(jsonObject.TryGetProperty(ItemsPropertyName, out var items))
        {
            var elementType = targetType.GetEnumerableElementType();
            var array = Array.CreateInstance(elementType, items.GetArrayLength());
            var index = 0;
            
            newOptions.Converters.Add(this);
            
            foreach (var element in items.EnumerateArray())
            {
                var deserializedElement = JsonSerializer.Deserialize(element.GetRawText(), elementType, newOptions)!;
                array.SetValue(deserializedElement, index++);
            }
            return array;
        }

        var json = jsonObject.GetRawText();
        var result = JsonSerializer.Deserialize(json, targetType, newOptions)!;
        return result;
    }

    private static object ReadPrimitive(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return (reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTimeOffset(out var datetime) => datetime,
            JsonTokenType.String => reader.GetString(),
            _ => JsonSerializer.Deserialize(ref reader, typeof(ExpandoObject), options)
        })!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        var type = value.GetType();

        newOptions.Converters.RemoveWhere(x => x is PolymorphicObjectConverterFactory);

        if (type.IsPrimitive || value is string or DateTimeOffset or JsonElement)
        {
            JsonSerializer.Serialize(writer, value, newOptions);
            return;
        }

        var jsonElement = JsonDocument.Parse(JsonSerializer.Serialize(value, type, newOptions)).RootElement;

        writer.WriteStartObject();

        if(jsonElement.ValueKind == JsonValueKind.Array)
        {
            writer.WritePropertyName(ItemsPropertyName);
            jsonElement.WriteTo(writer);
        }
        else
        {
            foreach (var property in jsonElement.EnumerateObject().Where(property => !property.NameEquals(TypePropertyName)))
            {
                property.WriteTo(writer);
            }
        }
        
        if(type != typeof(ExpandoObject))
        {
            // Write the type name so that we can reconstruct the actual type when deserializing.
            writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
        }
        
        writer.WriteEndObject();
    }
}