using Confluent.Kafka;
using Elsa.Kafka.Implementations;

namespace Elsa.Kafka.Factories;

public class DefaultConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumerDefinition = context.ConsumerDefinition;
        var config = consumerDefinition.Config;
        var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        return new ConsumerProxy(consumer);
    }
}