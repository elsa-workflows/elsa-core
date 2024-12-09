using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class ConsumerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public Type FactoryType { get; set; } = default!;
    public ConsumerConfig Config { get; set; } = new();
    public string? SchemaRegistryId { get; set; } = default!;
}