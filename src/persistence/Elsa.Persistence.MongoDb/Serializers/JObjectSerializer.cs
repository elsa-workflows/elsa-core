using System;
using Elsa.Serialization.Converters;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class JObjectSerializer : IBsonSerializer<JObject>
    {
        public static JObjectSerializer Instance { get; } = new();
        private readonly JsonSerializerSettings _serializerSettings;

        public JObjectSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            _serializerSettings.Converters.Add(new TypeJsonConverter());
        }

        public Type ValueType => typeof(JObject);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (JObject) value);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JObject value)
        {
            var json = JsonConvert.SerializeObject(value, _serializerSettings);
            context.Writer.WriteString(json);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

        public JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var text = context.Reader.ReadString();
            return JsonConvert.DeserializeObject<JObject>(text, _serializerSettings)!;
        }
    }
}