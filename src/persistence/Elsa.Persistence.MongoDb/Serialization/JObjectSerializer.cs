using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public class JObjectSerializer : SerializerBase<JObject>
    {
        public override JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            return JObject.Parse(document.ToString());
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
        {
            var document = BsonDocument.Parse(value.ToString());
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }
    }
}