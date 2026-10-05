using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static MongoDB.Bson.Serialization.BsonSerializer;

namespace Elsa.Persistence.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="JsonNode"/>.
/// </summary>
public class JsonNodeBsonConverter : JsonNodeBsonConverter<JsonNode>
{
    private static int _providerRegistered;

    /// <summary>
    /// Registers serializers for <see cref="JsonNode"/> and the concrete types
    /// <see cref="JsonObject"/>, <see cref="JsonArray"/>, and <see cref="JsonValue"/>.
    /// Also registers a serialization provider so internal <see cref="JsonValue"/>
    /// implementations are looked up without falling back to a class map.
    /// </summary>
    public static void RegisterSerializers()
    {
        if (Interlocked.CompareExchange(ref _providerRegistered, 1, 0) == 0)
            RegisterSerializationProvider(new JsonNodeBsonSerializationProvider());

        TryRegisterSerializer(typeof(JsonNode), new JsonNodeBsonConverter());
        TryRegisterSerializer(typeof(JsonObject), new JsonNodeBsonConverter<JsonObject>());
        TryRegisterSerializer(typeof(JsonArray), new JsonNodeBsonConverter<JsonArray>());
        TryRegisterSerializer(typeof(JsonValue), new JsonNodeBsonConverter<JsonValue>());
    }
}

/// <summary>
/// Serializes a <see cref="JsonNode"/> of the specified nominal type.
/// <see cref="ValueType"/> must match the registered type so
/// <c>BsonSerializer.LookupSerializer</c> (used by <see cref="PolymorphicSerializer"/>)
/// finds this converter for <see cref="JsonObject"/>, <see cref="JsonArray"/>, and <see cref="JsonValue"/>.
/// </summary>
public class JsonNodeBsonConverter<TNode> : IBsonSerializer<TNode> where TNode : JsonNode
{
    /// <inheritdoc />
    public Type ValueType => typeof(TNode);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TNode value)
    {
        SerializeJsonNode(context, value);
    }

    /// <inheritdoc />
    public TNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var node = DeserializeJsonNode(context);
        if (node is null)
            return null!;
        if (node is TNode typed)
            return typed;

        throw new BsonSerializationException($"Deserialized JsonNode of type '{node.GetType().FullName}' is not assignable to '{typeof(TNode).FullName}'.");
    }

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        SerializeJsonNode(context, (JsonNode)value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return DeserializeJsonNode(context)!;
    }

    private static void SerializeJsonNode(BsonSerializationContext context, JsonNode value)
    {
        if (value == null!)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteStartDocument();
        context.Writer.WriteName("type");

        switch (value)
        {
            case JsonObject jsonObject:
                context.Writer.WriteString("JsonObject");
                context.Writer.WriteName("value");
                context.Writer.WriteString(jsonObject.ToJsonString());
                break;

            case JsonArray jsonArray:
                context.Writer.WriteString("JsonArray");
                context.Writer.WriteName("value");
                context.Writer.WriteString(jsonArray.ToJsonString());
                break;

            case JsonValue jsonValue:
                context.Writer.WriteString("JsonValue");
                context.Writer.WriteName("value");
                if (jsonValue.TryGetValue(out string? stringValue))
                    context.Writer.WriteString(stringValue);
                else if (jsonValue.TryGetValue(out int intValue))
                    context.Writer.WriteInt32(intValue);
                else if (jsonValue.TryGetValue(out int longValue))
                    context.Writer.WriteInt64(longValue);
                else if (jsonValue.TryGetValue(out double doubleValue))
                    context.Writer.WriteDouble(doubleValue);
                else if (jsonValue.TryGetValue(out bool boolValue))
                    context.Writer.WriteBoolean(boolValue);
                else if (jsonValue.TryGetValue(out DateTimeOffset dateTimeOffsetValue))
                    context.Writer.WriteDateTime(dateTimeOffsetValue.ToUnixTimeMilliseconds());
                else if (jsonValue.TryGetValue(out DateTime dateTimeValue))
                    context.Writer.WriteDateTime(new DateTimeOffset(dateTimeValue).ToUnixTimeMilliseconds());
                else
                    throw new BsonSerializationException("Unsupported JsonValue type");
                break;

            default:
                throw new BsonSerializationException($"Unexpected JsonNode type: {value.GetType()}");
        }

        context.Writer.WriteEndDocument();
    }

    private static JsonNode? DeserializeJsonNode(BsonDeserializationContext context)
    {
        var reader = context.Reader;
        var bsonType = reader.GetCurrentBsonType();

        switch (bsonType)
        {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.Document:
                return DeserializeDocument(BsonDocumentSerializer.Instance.Deserialize(context));
            case BsonType.Array:
                return DeserializeLegacyArray(BsonArraySerializer.Instance.Deserialize(context));
            default:
                return DeserializePrimitiveJsonValue(ReadPrimitive(reader, bsonType));
        }
    }

    private static JsonNode DeserializeDocument(BsonDocument document)
    {
        if (IsTaggedJsonNode(document))
            return DeserializeTagged(document);

        // Legacy class-map / dictionary format written when only JsonNode was registered:
        // { "DialogId": { "type": "JsonValue", "value": "..." }, ... }
        var obj = new JsonObject();
        foreach (var element in document)
            obj[element.Name] = DeserializeBsonValue(element.Value);
        return obj;
    }

    private static bool IsTaggedJsonNode(BsonDocument document)
    {
        if (document.ElementCount != 2 || !document.Contains("type") || !document.Contains("value"))
            return false;

        if (document["type"].BsonType != BsonType.String)
            return false;

        return document["type"].AsString switch
        {
            "JsonObject" or "JsonArray" => document["value"].BsonType == BsonType.String,
            "JsonValue" => true,
            _ => false
        };
    }

    private static JsonNode DeserializeTagged(BsonDocument document)
    {
        var type = document["type"].AsString;
        var value = document["value"];

        switch (type)
        {
            case "JsonObject":
            case "JsonArray":
                return JsonNode.Parse(value.AsString)!;
            case "JsonValue":
                return DeserializePrimitiveJsonValue(value);
            default:
                throw new BsonSerializationException($"Unsupported JsonNode type: {type}");
        }
    }

    private static JsonNode? DeserializeBsonValue(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Null => null,
            BsonType.Document => DeserializeDocument(value.AsBsonDocument),
            BsonType.Array => DeserializeLegacyArray(value.AsBsonArray),
            _ => DeserializePrimitiveJsonValue(value)
        };
    }

    private static JsonArray DeserializeLegacyArray(BsonArray array)
    {
        var result = new JsonArray();
        foreach (var item in array)
            result.Add(DeserializeBsonValue(item));
        return result;
    }

    private static JsonValue DeserializePrimitiveJsonValue(BsonValue value)
    {
        switch (value.BsonType)
        {
            case BsonType.String:
                return JsonValue.Create(value.AsString)!;
            case BsonType.Int32:
                return JsonValue.Create(value.AsInt32)!;
            case BsonType.Int64:
                return JsonValue.Create(value.AsInt64)!;
            case BsonType.Double:
                return JsonValue.Create(value.AsDouble)!;
            case BsonType.Boolean:
                return JsonValue.Create(value.AsBoolean)!;
            case BsonType.DateTime:
                return JsonValue.Create(value.AsBsonDateTime.MillisecondsSinceEpoch)!;
            default:
                throw new BsonSerializationException($"Unsupported BSON type: {value.BsonType}");
        }
    }

    private static BsonValue ReadPrimitive(IBsonReader reader, BsonType bsonType)
    {
        return bsonType switch
        {
            BsonType.String => reader.ReadString(),
            BsonType.Int32 => reader.ReadInt32(),
            BsonType.Int64 => reader.ReadInt64(),
            BsonType.Double => reader.ReadDouble(),
            BsonType.Boolean => reader.ReadBoolean(),
            BsonType.DateTime => new BsonDateTime(reader.ReadDateTime()),
            _ => throw new BsonSerializationException($"Unsupported BSON type: {bsonType}")
        };
    }
}

/// <summary>
/// Provides <see cref="JsonNodeBsonConverter{TNode}"/> for any <see cref="JsonNode"/> subclass,
/// including internal <see cref="JsonValue"/> implementations whose runtime type is not
/// <see cref="JsonValue"/> itself.
/// </summary>
public class JsonNodeBsonSerializationProvider : IBsonSerializationProvider
{
    /// <inheritdoc />
    public IBsonSerializer? GetSerializer(Type type)
    {
        if (type is null || !typeof(JsonNode).IsAssignableFrom(type))
            return null;

        var serializerType = typeof(JsonNodeBsonConverter<>).MakeGenericType(type);
        return (IBsonSerializer)Activator.CreateInstance(serializerType)!;
    }
}
