using Confluent.Kafka;
using Elsa.Kafka.Implementations;

namespace Elsa.Kafka.Factories;

public class DefaultProducerFactory : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producer = new ProducerBuilder<Null, string>(workerContext.ProducerDefinition.Config).Build();
        return new ProducerProxy(producer);
    }
}