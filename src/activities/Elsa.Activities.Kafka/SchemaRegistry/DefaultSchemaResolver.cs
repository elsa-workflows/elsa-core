using Confluent.Kafka;
using System.Text;
using System.Threading.Tasks;

using Confluent.SchemaRegistry;
using Avro.Specific;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    public class DefaultSchemaResolver : ISchemaResolver
    {
        public ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, byte[]> message)
        {
            return new ValueTask<string>("");
        }
    }
}
