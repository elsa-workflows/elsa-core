using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="Type"/>.
/// </summary>
public class JsonElementSerializer : IBsonSerializer<JsonElement>
{
    /// <inheritdoc />
    public Type ValueType => typeof(JsonElement);
    
    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (JsonElement)value);
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

    /// <inheritdoc />
    public JsonElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return new JsonElement();
        }

        var json = context.Reader.ReadString();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonElement value)
    {
        context.Writer.WriteString(value.GetRawText());
    }
}