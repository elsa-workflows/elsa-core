using Confluent.Kafka;
using Elsa.Common.Entities;
using Elsa.Kafka.Factories;

namespace Elsa.Kafka;

public class ConsumerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public Type FactoryType { get; set; } = typeof(DefaultConsumerFactory);
    public ConsumerConfig Config { get; set; } = new();
}