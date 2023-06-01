using Elsa.Http.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDB.Serializers;

public class PolymorphicSerializer : IBsonSerializer<object>
{
    private readonly Dictionary<string, IBsonSerializer> _serializers = new Dictionary<string, IBsonSerializer>();

    public PolymorphicSerializer()
    {
        _serializers.Add(typeof(HttpEndpointBookmarkPayload).FullName, new HttpEndpointBookmarkPayloadSerializer());
    }

    public Type ValueType => typeof(object);

    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var typeValue = context.Reader.ReadString();
        if (_serializers.TryGetValue(typeValue, out var serializer))
        {
            return serializer.Deserialize(context, args);
        }
        throw new BsonSerializationException($"No serializer found for type {typeValue}");
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (_serializers.TryGetValue(value.GetType().FullName, out var serializer))
        {
            context.Writer.WriteString(value.GetType().FullName);
            serializer.Serialize(context, args, value);
        }
        else
        {
            throw new BsonSerializationException($"No serializer found for type {value.GetType().FullName}");
        }
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, value);
    }
}
