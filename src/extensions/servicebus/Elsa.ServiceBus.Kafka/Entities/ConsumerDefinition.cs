using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.ServiceBus.Kafka;

public class ConsumerDefinition : Entity
{
    public string Name { get; set; } = null!;
    public Type FactoryType { get; set; } = null!;
    public ConsumerConfig Config { get; set; } = new();
    public string? SchemaRegistryId { get; set; } = null!;
}