using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="FlowScope"/>.
/// </summary>
public class FlowScopeSerializer(IPayloadSerializer payloadSerializer) : IBsonSerializer<FlowScope>
{
    /// <inheritdoc />
    public Type ValueType => typeof(FlowScope);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (FlowScope)value);
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FlowScope value)
    {
        if (value is null)
            context.Writer.WriteNull();
        else
        {
            var json = payloadSerializer.Serialize(value);
            context.Writer.WriteString(json);
        }
    }

    /// <inheritdoc />
    public FlowScope Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bsonType = reader.GetCurrentBsonType();

        if (bsonType == BsonType.Null)
        {
            reader.ReadNull();
            return new();
        }
        
        if(bsonType == BsonType.String)
        {
            var json = context.Reader.ReadString();

            return string.IsNullOrEmpty(json) ? new() : payloadSerializer.Deserialize<FlowScope>(json);
        }

        return new();
    }
}