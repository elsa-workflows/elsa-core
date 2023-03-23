using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    internal interface ISchemaResolver
    {
        ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, byte[]> message);
    }
}
