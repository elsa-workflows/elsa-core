using System;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class ObjectSerializer : IBsonSerializer<object>
    {
        public static ObjectSerializer Instance { get; } = new();

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
        }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        public Type ValueType => typeof(object);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            var json = JsonConvert.SerializeObject(value, _serializerSettings);
            context.Writer.WriteString(json);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var text = context.Reader.ReadString();
            return JsonConvert.DeserializeObject<object>(text, _serializerSettings)!;
        }
    }
}