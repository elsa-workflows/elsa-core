using Elsa.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public abstract class JsonSerializerBase<T> : SerializerBase<T>
    {
        private readonly ITokenSerializer serializer;

        protected JsonSerializerBase(ITokenSerializer serializer)
        {
            this.serializer = serializer;
        }
        
        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            var value = serializer.Deserialize<T>(document.ToString());

            return value;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            var json = value != null ? serializer.Serialize(value) : null;
            var document = json != null ? BsonDocument.Parse(json.ToString(Formatting.None)) : new BsonDocument();
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }
    }
}