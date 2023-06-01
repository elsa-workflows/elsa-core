using Elsa.Http.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Elsa.MongoDB.Serializers;

public class HttpEndpointBookmarkPayloadSerializer : IBsonSerializer<HttpEndpointBookmarkPayload>
{
    public Type ValueType => typeof(HttpEndpointBookmarkPayload);

    public HttpEndpointBookmarkPayload Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var path = context.Reader.ReadString();
        var method = context.Reader.ReadString();
        var authorize = context.Reader.ReadBsonType() == BsonType.Boolean ? (bool?)context.Reader.ReadBoolean() : null;
        var policy = context.Reader.ReadBsonType() == BsonType.String ? context.Reader.ReadString() : null;

        return new HttpEndpointBookmarkPayload(path, method, authorize, policy);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HttpEndpointBookmarkPayload value)
    {
        context.Writer.WriteName("Path");
        context.Writer.WriteString(value.Path);
        context.Writer.WriteName("Method");
        context.Writer.WriteString(value.Method);
        context.Writer.WriteName("Authorize");
        
        if (value.Authorize.HasValue)
            context.Writer.WriteBoolean(value.Authorize.Value);
        else
            context.Writer.WriteNull();
        
        context.Writer.WriteName("Policy");
        if (value.Policy != null)
            context.Writer.WriteString(value.Policy);
        else
            context.Writer.WriteNull();
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (HttpEndpointBookmarkPayload)value);
    }
}
