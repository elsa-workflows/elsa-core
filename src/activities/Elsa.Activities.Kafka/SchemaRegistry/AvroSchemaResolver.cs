using Confluent.Kafka;
using System.Text;
using System.Threading.Tasks;

using Confluent.SchemaRegistry;
using Avro.Generic;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    public class AvroSchemaResolver : ISchemaResolver
    {
        ISchemaRegistryClient _schemaRegistryClient;

        public AvroSchemaResolver(ISchemaRegistryClient schemaRegistryClient)
        {
            _schemaRegistryClient = schemaRegistryClient;
        }


        public async ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, string> message)
        {
            if (_schemaRegistryClient is not null)
            {
                var deserializer = new Confluent.SchemaRegistry.Serdes.AvroDeserializer<GenericRecord>(_schemaRegistryClient);
                var res = await deserializer.DeserializeAsync(Encoding.ASCII.GetBytes(message.Value), false, SerializationContext.Empty);
                return res.Schema.Name;
            }
            return "";
        }
    }
}
