using Confluent.Kafka;
using Elsa.ServiceBus.Kafka.Implementations;

namespace Elsa.ServiceBus.Kafka.Factories;

public class DefaultProducerFactory : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producer = new ProducerBuilder<Null, string>(workerContext.ProducerDefinition.Config).Build();
        return new ProducerProxy(producer);
    }
}