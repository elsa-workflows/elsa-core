using Confluent.Kafka;
using Elsa.ServiceBus.Kafka.Implementations;

namespace Elsa.ServiceBus.Kafka.Factories;

public class DefaultConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumer = new ConsumerBuilder<Ignore, string>(context.ConsumerDefinition.Config).Build();
        return new ConsumerProxy(consumer);
    }
}