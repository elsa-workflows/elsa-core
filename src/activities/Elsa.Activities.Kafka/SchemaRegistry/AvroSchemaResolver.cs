using Confluent.Kafka;
using System.Text;
using System.Threading.Tasks;

using Confluent.SchemaRegistry;
using Avro.Generic;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    public class AvroSchemaResolver : ISchemaResolver
    {
        private readonly ISchemaRegistryClient _schemaRegistryClient;
        private readonly Confluent.SchemaRegistry.Serdes.AvroDeserializer<GenericRecord> _deserializer;

        public AvroSchemaResolver(ISchemaRegistryClient schemaRegistryClient)
        {
            _schemaRegistryClient = schemaRegistryClient;
            _deserializer = new Confluent.SchemaRegistry.Serdes.AvroDeserializer<GenericRecord>(_schemaRegistryClient);
        }


        public async ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, string> message)
        {
            if (_schemaRegistryClient is not null)
            {
                var res = await _deserializer.DeserializeAsync(Encoding.ASCII.GetBytes(message.Value), false, SerializationContext.Empty);
                return res.Schema.Name;
            }
            return "";
        }
    }
}
