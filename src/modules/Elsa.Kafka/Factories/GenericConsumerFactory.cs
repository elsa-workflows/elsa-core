using Confluent.Kafka;
using Elsa.Kafka.Implementations;
using Elsa.Kafka.Serializers;

namespace Elsa.Kafka.Factories;

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