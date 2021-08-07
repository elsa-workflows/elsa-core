using System;
using Elsa.Models;
using Elsa.Serialization.Converters;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.MongoDb.Serializers
{
    public class VariablesSerializer : IBsonSerializer<Variables>
    {
        public static VariablesSerializer Instance { get; } = new();
        private readonly JsonSerializerSettings _serializerSettings;

        public VariablesSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto,
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            
            _serializerSettings.Converters.Add(new TypeJsonConverter());
        }
        
        public Type ValueType => typeof(Variables);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (Variables) value);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Variables value)
        {
            var json = JsonConvert.SerializeObject(value, _serializerSettings);
            context.Writer.WriteString(json);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

        public Variables Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var text = context.Reader.ReadString();
            return JsonConvert.DeserializeObject<Variables>(text, _serializerSettings)!;
        }
    }
}