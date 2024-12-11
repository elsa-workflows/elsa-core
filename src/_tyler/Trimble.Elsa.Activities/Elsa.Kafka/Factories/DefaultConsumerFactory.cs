using Confluent.Kafka;
using Elsa.Kafka.Implementations;

namespace Elsa.Kafka.Factories;

public class DefaultConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumer = new ConsumerBuilder<Ignore, string>(context.ConsumerDefinition.Config).Build();
        return new ConsumerProxy(consumer);
    }
}