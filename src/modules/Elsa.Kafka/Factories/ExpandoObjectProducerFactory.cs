using System.Dynamic;
using Confluent.Kafka;
using Elsa.Kafka.Implementations;
using Elsa.Kafka.Serializers;

namespace Elsa.Kafka.Factories;

public class ExpandoObjectProducerFactory : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producer = new ProducerBuilder<Null, ExpandoObject>(workerContext.ProducerDefinition.Config)
            .SetValueSerializer(new JsonSerializer<ExpandoObject>())
            .Build();
        return new ProducerProxy(producer);
    }
}