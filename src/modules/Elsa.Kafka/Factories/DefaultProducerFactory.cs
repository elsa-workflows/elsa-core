using Confluent.Kafka;
using Elsa.Kafka.Implementations;

namespace Elsa.Kafka.Factories;

public class DefaultProducerFactory : IProducerFactory
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producerDefinition = workerContext.ProducerDefinition;
        var config = producerDefinition.Config;
        var producer = new ProducerBuilder<Null, string>(config).Build();
        return new ProducerProxy(producer);
    }
}