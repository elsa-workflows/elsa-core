using Confluent.Kafka;
using Elsa.Common.Entities;
using Elsa.Kafka.Factories;

namespace Elsa.Kafka;

public class ProducerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public Type FactoryType { get; set; } = default!;
    public ProducerConfig Config { get; set; } = new();
}