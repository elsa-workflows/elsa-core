using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public class JObjectSerializer : SerializerBase<JObject>
    {
        public override JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            var settings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return JsonConvert.DeserializeObject<JObject>(document.ToString(), settings);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
        {
            var document = BsonDocument.Parse(value.ToString());
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }
    }
}