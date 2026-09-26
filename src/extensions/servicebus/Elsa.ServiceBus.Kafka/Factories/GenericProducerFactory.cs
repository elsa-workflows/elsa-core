using Confluent.Kafka;
using Elsa.ServiceBus.Kafka.Implementations;
using Elsa.ServiceBus.Kafka.Serializers;

namespace Elsa.ServiceBus.Kafka.Factories;

public class GenericProducerFactory<TKey, TValue> : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producer = new ProducerBuilder<TKey, TValue>(workerContext.ProducerDefinition.Config)
            .SetValueSerializer(new JsonSerializer<TValue>())
            .Build();
        return new ProducerProxy(producer);
    }
}