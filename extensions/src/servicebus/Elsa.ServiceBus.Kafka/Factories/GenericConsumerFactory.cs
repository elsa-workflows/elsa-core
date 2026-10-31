using Confluent.Kafka;
using Elsa.ServiceBus.Kafka.Implementations;
using Elsa.ServiceBus.Kafka.Serializers;

namespace Elsa.ServiceBus.Kafka.Factories;

public class GenericConsumerFactory<TKey, TValue> : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumer = new ConsumerBuilder<TKey, TValue>(context.ConsumerDefinition.Config)
            .SetValueDeserializer(new JsonDeserializer<TValue>())
            .Build();
        return new ConsumerProxy(consumer);
    }
}