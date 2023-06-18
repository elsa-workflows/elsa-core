using Elsa.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

public class TypeSerializer : IBsonSerializer<Type>
{
    public Type ValueType => typeof(Type);

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    public Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }

        var typeAsString = context.Reader.ReadString();
        return Type.GetType(typeAsString);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteString(value.GetSimpleAssemblyQualifiedName());
        }
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is Type typeValue)
        {
            Serialize(context, args, typeValue);
        }
        else if (value == null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            throw new ArgumentException("Expected value to be of type 'Type'", nameof(value));
        }
    }
}