using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Reads objects as primitive types rather than <see cref="JsonElement"/> values while also maintaining the .NET type name for reconstructing the actual type.
/// </summary>
public class PolymorphicObjectConverter : JsonConverter<object>
{
    private const string TypePropertyName = "_type";
    private const string ItemsPropertyName = "_items";
    private const string IdPropertyName = "$id";
    private const string RefPropertyName = "$ref";
    private const string ValuesPropertyName = "$values";

    /// <inheritdoc />
    public PolymorphicObjectConverter()
    {
    }

    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);

        if (reader.TokenType != JsonTokenType.StartObject)
            return ReadPrimitive(ref reader, newOptions);

        var targetType = ReadType(reader);
        if (targetType == null)
            return ReadObject(ref reader, newOptions);

        // If the target type is not an IEnumerable, or is a dictionary, deserialize the object directly.
        var isEnumerable = typeof(IEnumerable).IsAssignableFrom(targetType);

        if (!isEnumerable)
            return JsonSerializer.Deserialize(ref reader, targetType, newOptions)!;

        var isDictionary = typeof(IDictionary).IsAssignableFrom(targetType);
        if (isDictionary)
        {
            // Remove the _type property name from the JSON, if any.
            var parsedModel = (JsonObject)JsonNode.Parse(ref reader)!;
            parsedModel.Remove(TypePropertyName);
            return parsedModel.Deserialize(targetType, newOptions)!;
        }

        // Otherwise, deserialize the object as an array.
        var elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GenericTypeArguments.FirstOrDefault() ?? typeof(object);
        if (elementType == null)
            throw new InvalidOperationException($"Cannot determine the element type of array '{targetType}'.");

        var model = JsonElement.ParseValue(ref reader);
        var referenceResolver = (options.ReferenceHandler as CrossScopedReferenceHandler)?.GetResolver();

        if (model.TryGetProperty(RefPropertyName, out var refProperty))
        {
            var refId = refProperty.GetString()!;
            return referenceResolver?.ResolveReference(refId)!;
        }

        var values = model.TryGetProperty(ItemsPropertyName, out var itemsProp) ? itemsProp.EnumerateArray().ToList() : model.GetProperty(ValuesPropertyName).EnumerateArray().ToList();
        var id = model.TryGetProperty(IdPropertyName, out var idProp) ? idProp.GetString() : default;
        var collection = targetType.IsArray ? Array.CreateInstance(elementType, values.Count) : (IList)Activator.CreateInstance(targetType)!;
        var index = 0;

        if (id != null)
            referenceResolver?.AddReference(id, collection);

        foreach (var element in values)
        {
            var deserializedElement = JsonSerializer.Deserialize(JsonSerializer.Serialize(element), elementType, newOptions)!;
            if (collection is Array array)
            {
                array.SetValue(deserializedElement, index++);
            }
            else
            {
                collection.Add(deserializedElement);
            }
        }

        return collection;
    }

    private static Type? ReadType(Utf8JsonReader reader)
    {
        reader.Read(); // Move to the first token inside the object.
        string? typeName = null;

        // Read while we haven't reached the end of the object.
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            // If we find the _type property, read its value and break out of the loop.
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals(TypePropertyName))
            {
                reader.Read(); // Move to the value of the _type property
                typeName = reader.GetString();
                break;
            }

            // Skip through nested objects and arrays.
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                var depth = 1;

                while (depth > 0 && reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartObject:
                        case JsonTokenType.StartArray:
                            depth++;
                            break;
                        case JsonTokenType.EndObject:
                        case JsonTokenType.EndArray:
                            depth--;
                            break;
                    }
                }
            }

            reader.Read(); // Move to the next token
        }

        // If we found the _type property, attempt to resolve the type.
        var targetType = typeName != null ? Type.GetType(typeName) : default;
        return targetType;
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
            _ => throw new JsonException("Not a primitive type.")
        })!;
    }

    private object ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
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
                var dict = new ExpandoObject() as IDictionary<string, object>;
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
                                var value = Read(ref reader, typeof(object), options);
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

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        var type = value.GetType();

        if (type.IsPrimitive || value is string or DateTimeOffset or DateTime or DateOnly or TimeOnly or JsonElement or Guid or TimeSpan or Uri or Version or Enum)
        {
            // Remove the converter so that we don't end up in an infinite loop.
            newOptions.Converters.RemoveWhere(x => x is PolymorphicObjectConverterFactory);

            // Serialize the value directly.
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
                property.WriteTo(writer);
        }

        if (type != typeof(ExpandoObject))
        {
            // Write the type name so that we can reconstruct the actual type when deserializing.
            writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
        }

        writer.WriteEndObject();
    }
}