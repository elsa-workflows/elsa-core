using System.Dynamic;
using Confluent.Kafka;
using Elsa.Kafka.Implementations;
using Elsa.Kafka.Serializers;

namespace Elsa.Kafka.Factories;

public class ExpandoObjectConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var consumer = new ConsumerBuilder<Ignore, ExpandoObject>(context.ConsumerDefinition.Config)
            .SetValueDeserializer(new JsonDeserializer<ExpandoObject>())
            .Build();
        return new ConsumerProxy(consumer);
    }
}