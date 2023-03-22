using Confluent.Kafka;
using System.Text;
using System.Threading.Tasks;

using Confluent.SchemaRegistry;
using Avro.Specific;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    public class DefaultSchemaResolver : ISchemaResolver
    {
        ISchemaRegistryClient _schemaRegistryClient;

        public DefaultSchemaResolver(ISchemaRegistryClient schemaRegistryClient)
        {
            _schemaRegistryClient = schemaRegistryClient;
        }        

        public ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, string> message)
        {
            return new ValueTask<string>("");
        }
    }
}
