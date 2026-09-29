using System.Dynamic;
using Confluent.Kafka;
using Elsa.ServiceBus.Kafka.Implementations;
using Elsa.ServiceBus.Kafka.Serializers;

namespace Elsa.ServiceBus.Kafka.Factories;

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