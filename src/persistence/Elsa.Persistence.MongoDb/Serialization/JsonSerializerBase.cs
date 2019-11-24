using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public abstract class JsonSerializerBase<T> : SerializerBase<T>
    {
        private readonly JsonSerializerSettings serializerSettings;

        protected JsonSerializerBase()
        {
            serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        }
        
        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            return JsonConvert.DeserializeObject<T>(document.ToString(), serializerSettings);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            var json = value != null ? JsonConvert.SerializeObject(value, serializerSettings) : null;
            var document = json != null ? BsonDocument.Parse(json) : new BsonDocument();
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }
    }
}