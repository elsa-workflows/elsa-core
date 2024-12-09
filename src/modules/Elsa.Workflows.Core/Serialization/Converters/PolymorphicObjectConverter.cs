using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Serialization.ReferenceHandlers;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Reads objects as primitive types rather than <see cref="JsonElement"/> values while also maintaining the .NET type name for reconstructing the actual type.
/// </summary>
public class PolymorphicObjectConverter(IWellKnownTypeRegistry wellKnownTypeRegistry) : JsonConverter<object>
{
    private const string TypePropertyName = "_type";
    private const string ItemsPropertyName = "_items";
    private const string IslandPropertyName = "_island";
    private const string IdPropertyName = "$id";
    private const string RefPropertyName = "$ref";
    private const string ValuesPropertyName = "$values";

    /// <inheritdoc />
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = options.Clone();

        if (reader.TokenType != JsonTokenType.StartObject && reader.TokenType != JsonTokenType.StartArray)
            return ReadPrimitive(ref reader, newOptions);

        var targetType = ReadType(reader, options);
        if (targetType == null)
            return ReadObject(ref reader, newOptions);

        // If the target type is not an IEnumerable, or is a dictionary, deserialize the object directly.
        var isEnumerable = typeof(IEnumerable).IsAssignableFrom(targetType);

        if (!isEnumerable)
        {
            try
            {
                return JsonSerializer.Deserialize(ref reader, targetType, newOptions)!;
            }
            catch (Exception e) when (e is NotSupportedException or TargetException)
            {
                return default!;
            }
        }

        // If the target type is a Newtonsoft.JObject, parse the JSON island.
        var isNewtonsoftObject = targetType == typeof(JObject);

        if (isNewtonsoftObject)
        {
            var parsedModel = JsonElement.ParseValue(ref reader);
            var newtonsoftJson = parsedModel.GetProperty(IslandPropertyName).GetString();
            return !string.IsNullOrWhiteSpace(newtonsoftJson) ? JObject.Parse(newtonsoftJson) : new JObject();
        }

        // If the target type is a Newtonsoft.JArray, parse the JSON island.
        var isNewtonsoftArray = targetType == typeof(JArray);

        if (isNewtonsoftArray)
        {
            var parsedModel = JsonElement.ParseValue(ref reader);
            var newtonsoftJson = parsedModel.GetProperty(IslandPropertyName).GetString();
            return !string.IsNullOrWhiteSpace(newtonsoftJson) ? JArray.Parse(newtonsoftJson) : new JArray();
        }

        // If the target type is a System.Text.JsonObject, parse the JSON island.
        var isJsonObject = targetType == typeof(JsonObject);

        if (isJsonObject)
        {
            var parsedModel = JsonElement.ParseValue(ref reader);
            var systemTextJson = parsedModel.GetProperty(IslandPropertyName).GetString();
            return !string.IsNullOrWhiteSpace(systemTextJson) ? JsonObject.Parse(systemTextJson) : new JsonObject();
        }

        var isJsonArray = targetType == typeof(JsonArray);

        if (isJsonArray)
        {
            var parsedModel = JsonElement.ParseValue(ref reader);
            var systemTextJson = parsedModel.GetProperty(IslandPropertyName).GetString();
            return !string.IsNullOrWhiteSpace(systemTextJson) ? JsonArray.Parse(systemTextJson) : new JsonArray();
        }

        var isDictionary = typeof(IDictionary).IsAssignableFrom(targetType);
        if (isDictionary)
        {
            // Remove the _type property name from the JSON, if any.
            var parsedNode = JsonNode.Parse(ref reader)!;
            if (parsedNode is JsonObject parsedModel) parsedModel.Remove(TypePropertyName);
            return parsedNode.Deserialize(targetType, newOptions)!;
        }

        var isCollection = typeof(ICollection).IsAssignableFrom(targetType);

        // Otherwise, deserialize the object as an array.
        var elementType = targetType.IsArray
            ? targetType.GetElementType()
            : targetType.GenericTypeArguments.FirstOrDefault() ??
              (isCollection // Could be a class derived from Collection<T> or List<T>.
                  ? targetType.BaseType?.GenericTypeArguments[0]
                  : targetType.GenericTypeArguments.FirstOrDefault()
                    ?? typeof(object));
        if (elementType == null)
            throw new InvalidOperationException($"Cannot determine the element type of array '{targetType}'.");

        var model = JsonElement.ParseValue(ref reader);
        var referenceResolver = (newOptions.ReferenceHandler as CrossScopedReferenceHandler)?.GetResolver();

        if (model.TryGetProperty(RefPropertyName, out var refProperty))
        {
            var refId = refProperty.GetString()!;
            return referenceResolver?.ResolveReference(refId)!;
        }

        var values = model.TryGetProperty(ItemsPropertyName, out var itemsProp) ? itemsProp.EnumerateArray().ToList() : model.GetProperty(ValuesPropertyName).EnumerateArray().ToList();
        var id = model.TryGetProperty(IdPropertyName, out var idProp) ? idProp.GetString() : default;
        var collection = targetType.IsArray ? Array.CreateInstance(elementType, values.Count) : Activator.CreateInstance(targetType)!;
        var index = 0;

        if (id != null)
            referenceResolver?.AddReference(id, collection);

        var isHashSet = targetType.GenericTypeArguments.Length == 1 && typeof(ISet<>).MakeGenericType(targetType.GenericTypeArguments[0]).IsAssignableFrom(targetType);
        var addSetMethod = targetType.GetMethod("Add", [elementType])!;

        foreach (var element in values)
        {
            var deserializedElement = JsonSerializer.Deserialize(JsonSerializer.Serialize(element), elementType, newOptions)!;
            if (collection is Array array)
            {
                array.SetValue(deserializedElement, index++);
            }
            else if (isHashSet)
            {
                addSetMethod.Invoke(collection, [
                    deserializedElement
                ]);
            }
            else if (collection is IList list)
            {
                list.Add(deserializedElement);
            }
        }

        return collection;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null!)
        {
            writer.WriteNullValue();
            return;
        }

        var newOptions = options.Clone();
        var type = value.GetType();

        // If the type is a primitive type or an enumerable of a primitive type, serialize the value directly.
        bool IsPrimitive(Type valueType)
        {
            return type.IsPrimitive
                   || valueType == typeof(string)
                   || valueType == typeof(decimal)
                   || valueType == typeof(DateTimeOffset)
                   || valueType == typeof(DateTime)
                   || valueType == typeof(DateOnly)
                   || valueType == typeof(TimeOnly)
                   || valueType == typeof(JsonElement)
                   || valueType == typeof(Guid)
                   || valueType == typeof(TimeSpan)
                   || valueType == typeof(Uri)
                   || valueType == typeof(Version)
                   || valueType.IsEnum;
        }

        if (IsPrimitive(type))
        {
            // Remove the converter so that we don't end up in an infinite loop.
            newOptions.Converters.RemoveWhere(x => x is PolymorphicObjectConverterFactory);

            // Serialize the value directly.
            JsonSerializer.Serialize(writer, value, newOptions);
            return;
        }

        // Special case for Newtonsoft.Json and System.Text.Json types.
        // Newtonsoft.Json types are not supported by the System.Text.Json serializer and should be written as a string instead.
        // We include metadata about the type so that we can deserialize it later.
        if (type == typeof(JObject) || type == typeof(JArray) || type == typeof(JsonObject) || type == typeof(JsonArray))
        {
            writer.WriteStartObject();
            writer.WriteString(IslandPropertyName, value.ToString());
            writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
            writer.WriteEndObject();
            return;
        }

        // Determine if the value is going to be serialized for the first time.
        // Later on, we need to know this information to determine if we need to write the type name or not, so that we can reconstruct the actual type when deserializing.
        var shouldWriteTypeField = true;
        var referenceResolver = (CustomPreserveReferenceResolver?)(newOptions.ReferenceHandler as CrossScopedReferenceHandler)?.GetResolver();

        if (referenceResolver != null)
        {
            var exists = referenceResolver.HasReference(value);
            shouldWriteTypeField = !exists;
        }

        // Before we serialize the value, check to see if it's an ExpandoObject.
        // If it is, we need to sanitize its property names, because they can contain invalid characters.
        if (value is ExpandoObject)
        {
            var sanitized = new ExpandoObject();
            var dictionary = (IDictionary<string, object?>)sanitized;
            var expando = (IDictionary<string, object?>)value;

            foreach (var kvp in expando)
            {
                var key = EscapeKey(kvp.Key);
                dictionary[key] = kvp.Value;
            }

            value = sanitized;
        }

        var jsonElement = JsonDocument.Parse(JsonSerializer.Serialize(value, type, newOptions)).RootElement;

        // If the value is a string, serialize it directly.
        if (jsonElement.ValueKind == JsonValueKind.String)
        {
            // Serialize the value directly.
            JsonSerializer.Serialize(writer, jsonElement, newOptions);
            return;
        }

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
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }

        if (type != typeof(ExpandoObject))
        {
            if (shouldWriteTypeField)
            {
                if (newOptions.Converters.OfType<TypeJsonConverter>().FirstOrDefault() is { } typeJsonConverter)
                {
                    writer.WritePropertyName(TypePropertyName);
                    typeJsonConverter.Write(writer, type, newOptions);
                }
                else
                {
                    writer.WriteString(TypePropertyName, type.GetSimpleAssemblyQualifiedName());
                }
            }
        }

        writer.WriteEndObject();
    }

    private Type? ReadType(Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        reader.Read(); // Move to the first token inside the object.
        string? typeName = null;

        // Read while we haven't reached the end of the object.
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            // If we find the _type property, read its value and break out of the loop.
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals(TypePropertyName))
            {
                reader.Read(); // Move to the value of the _type property
                if (options.Converters.OfType<TypeJsonConverter>().FirstOrDefault() is { } typeJsonConverter)
                {
                    return typeJsonConverter.Read(ref reader, typeof(Type), options);
                }
                else
                {
                    typeName = reader.GetString();
                }
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
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
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
                var referenceResolver = (CustomPreserveReferenceResolver)(options.ReferenceHandler as CrossScopedReferenceHandler)?.GetResolver()!;
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.EndObject:
                            // If the object contains a single entry with a key of $ref, return the referenced object.
                            if (dict.Count == 1 && dict.TryGetValue(RefPropertyName, out var referencedObject))
                                return referencedObject;
                            return dict;

                        case JsonTokenType.PropertyName:
                            var key = reader.GetString()!;
                            reader.Read();
                            if (referenceResolver != null && key == RefPropertyName)
                            {
                                var referenceId = reader.GetString();
                                var reference = referenceResolver.ResolveReference(referenceId!);
                                dict.Add(key, reference);
                            }
                            else if (referenceResolver != null && key == IdPropertyName)
                            {
                                var referenceId = reader.GetString()!;

                                // Attempt to add the reference; if not found, we can ignore it and assume that the user is using the $id property for something else, such as in JSON $schema. 
                                referenceResolver.TryAddReference(referenceId, dict);
                            }
                            else
                            {
                                var value = Read(ref reader, typeof(object), options);
                                var unescapedKey = UnescapeKey(key);
                                dict.Add(unescapedKey, value);
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

    private string EscapeKey(string key)
    {
        return key.Replace("$", @"\\$");
    }

    private string UnescapeKey(string key)
    {
        return key.Replace(@"\\$", "$");
    }
}