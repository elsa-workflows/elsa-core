using Elsa.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="Type"/>.
/// </summary>
public class TypeSerializer : IBsonSerializer<Type>
{
    /// <inheritdoc />
    public Type ValueType => typeof(Type);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (Type)value);
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

    /// <inheritdoc />
    public Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null!;
        }

        var typeAsString = context.Reader.ReadString();
        return Type.GetType(typeAsString)!;
    }

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
    {
        if (value == null!)
            context.Writer.WriteNull();
        else
            context.Writer.WriteString(value.GetSimpleAssemblyQualifiedName());
    }
}