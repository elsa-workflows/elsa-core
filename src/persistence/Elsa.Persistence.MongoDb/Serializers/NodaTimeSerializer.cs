using System;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using NodaTime.Text;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class NodaTimeSerializer : IBsonSerializer<NodaTime.Instant>
    {
        public static NodaTimeSerializer Instance { get; } = new NodaTimeSerializer();

        public Type ValueType => typeof(NodaTime.Instant);

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, NodaTime.Instant value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            context.Writer.WriteString(InstantPattern.CreateWithInvariantCulture("g").Format(value));
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Serialize(context, args, (NodaTime.Instant)value);
        }

        public NodaTime.Instant Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.String)
            {
                throw new InvalidOperationException($"Cannot deserialize NodaTime.Instant from BsonType {bsonType}.");
            }

            var nodaTimeString = context.Reader.ReadString();
            var parseResult = InstantPattern.CreateWithInvariantCulture("g").Parse(nodaTimeString);

            if (!parseResult.Success)
            {
                throw parseResult.Exception;
            }

            return parseResult.Value;
        }
    }
}
