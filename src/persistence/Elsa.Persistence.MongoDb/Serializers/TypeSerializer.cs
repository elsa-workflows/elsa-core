using System;
using MongoDB.Bson.Serialization;
using Rebus.Extensions;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class TypeSerializer : IBsonSerializer<Type>
    {
        public static TypeSerializer Instance { get; } = new();
        public Type ValueType => typeof(Type);
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (Type) value);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
        {
            var typeName = value.GetSimpleAssemblyQualifiedName();
            context.Writer.WriteString(typeName);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

        public Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var typeName = context.Reader.ReadString();
            return typeName is not null and not "" ? Type.GetType(typeName)! : default!;
        }
    }
}