using System;

using MongoDB.Bson.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class JsonSerializer : IBsonSerializer<JObject>
    {
        public static JsonSerializer Instance { get; } = new JsonSerializer();

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        public System.Type ValueType => typeof(JObject);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, (JObject)value);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
        {
            if ((value is JObject jObject) == false)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var json = JsonConvert.SerializeObject(value, serializerSettings);
            context.Writer.WriteString(json);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var jObjectStr = context.Reader.ReadString();

            return JsonConvert.DeserializeObject<JObject>(jObjectStr, serializerSettings);
        }
    }
}
