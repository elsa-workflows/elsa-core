using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="JsonNode"/>.
/// </summary>
public class JsonNodeBsonConverter : IBsonSerializer<JsonNode>
{
    /// <inheritdoc />
    public Type ValueType => typeof(JsonNode);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode value)
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

    public JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        var type = context.Reader.ReadString();
        context.Reader.ReadName(Utf8NameDecoder.Instance);

        JsonNode result;
        switch (type)
        {
            case "JsonObject":
                var jsonObjectString = context.Reader.ReadString();
                result = JsonNode.Parse(jsonObjectString);
                break;

            case "JsonArray":
                var jsonArrayString = context.Reader.ReadString();
                result = JsonNode.Parse(jsonArrayString);
                break;

            case "JsonValue":
                var bsonType = context.Reader.GetCurrentBsonType();
                switch (bsonType)
                {
                    case BsonType.String:
                        result = JsonValue.Create(context.Reader.ReadString());
                        break;
                    case BsonType.Int32:
                        result = JsonValue.Create(context.Reader.ReadInt32());
                        break;
                    case BsonType.Int64:
                        result = JsonValue.Create(context.Reader.ReadInt64());
                        break;
                    case BsonType.Double:
                        result = JsonValue.Create(context.Reader.ReadDouble());
                        break;
                    case BsonType.Boolean:
                        result = JsonValue.Create(context.Reader.ReadBoolean());
                        break;
                    case BsonType.DateTime:
                        result = JsonValue.Create(context.Reader.ReadDateTime());
                        break;
                    default:
                        throw new BsonSerializationException($"Unsupported BSON type: {bsonType}");
                }
                break;

            default:
                throw new BsonSerializationException($"Unsupported JsonNode type: {type}");
        }

        context.Reader.ReadEndDocument();
        return result!;
    }

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (JsonNode)value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}

