using System.Dynamic;
using System.Linq.Expressions;
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

        var tmpReader = reader;
        using var jsonDocument = JsonDocument.ParseValue(ref tmpReader);
        var jsonObject = jsonDocument.RootElement;

        
        var copyReader = reader; // Create a copy of the reader
        copyReader.Read(); // Move to the first token inside the object

        string? typeName = null;

        while (copyReader.TokenType != JsonTokenType.EndObject)
        {
            if (copyReader.TokenType == JsonTokenType.PropertyName && copyReader.ValueTextEquals(TypePropertyName))
            {
                copyReader.Read(); // Move to the value of the _type property
                typeName = copyReader.GetString();
                break;
            }

            if (copyReader.TokenType == JsonTokenType.StartObject || copyReader.TokenType == JsonTokenType.StartArray)
            {
                int depth = 1;

                while (depth > 0 && copyReader.Read())
                {
                    if (copyReader.TokenType == JsonTokenType.StartObject || copyReader.TokenType == JsonTokenType.StartArray)
                    {
                        depth++;
                    }
                    else if (copyReader.TokenType == JsonTokenType.EndObject || copyReader.TokenType == JsonTokenType.EndArray)
                    {
                        depth--;
                    }
                }
            }

            copyReader.Read(); // Move to the next token
        }

        var targetType = typeName != null ? Type.GetType(typeName) : default;
        
        if (targetType == null)
        {
            //newOptions.Converters.RemoveWhere(x => x is PolymorphicObjectConverterFactory);
            //var value = JsonSerializer.Deserialize(ref reader, typeof(ExpandoObject), newOptions)!;
            newOptions.Converters.Add(new ExpandoObjectConverter());
            var value = JsonSerializer.Deserialize(ref reader, typeof(object), newOptions)!;
            return value;
        }

        if (!targetType.IsArray) 
            return JsonSerializer.Deserialize(ref reader, targetType, newOptions)!;
        
        var elementType = targetType.GetElementType();
        if (elementType == null)
        {
            throw new InvalidOperationException($"Cannot determine the element type of array '{targetType}'.");
        }

        var jsonArray = JsonSerializer.Deserialize(ref reader, typeof(List<object>), newOptions)! as List<object>;
        var array = Array.CreateInstance(elementType, jsonArray!.Count);
        var index = 0;

        newOptions.Converters.Add(this);

        foreach (var element in jsonArray)
        {
            //var deserializedElement = JsonSerializer.Deserialize(JsonSerializer.Serialize(element), elementType, newOptions)!;
            //array.SetValue(deserializedElement, index++);
            array.SetValue(element, index++);
        }

        return array;

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

        if (type.IsPrimitive || value is string or DateTimeOffset or DateTime or DateOnly or TimeOnly or JsonElement or Guid or TimeSpan or Uri or Version or Enum)
        {
            JsonSerializer.Serialize(writer, value, newOptions);
            return;
        }

        var jsonElement = JsonDocument.Parse(JsonSerializer.Serialize(value, type, newOptions)).RootElement;

        writer.WriteStartObject();

        if (jsonElement.ValueKind == JsonValueKind.Array)
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
        
        if (type != typeof(ExpandoObject))
        {
            // Write the type name so that we can reconstruct the actual type when deserializing.
            writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
        }
        
        // // Write the type name so that we can reconstruct the actual type when deserializing.
        // writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
        
        writer.WriteEndObject();
    }
}