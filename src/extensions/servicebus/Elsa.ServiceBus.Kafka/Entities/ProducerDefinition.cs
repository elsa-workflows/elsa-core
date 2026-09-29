using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.ServiceBus.Kafka;

public class ProducerDefinition : Entity
{
    public string Name { get; set; } = null!;
    public Type FactoryType { get; set; } = null!;
    public ProducerConfig Config { get; set; } = new();
    public string? SchemaRegistryId { get; set; } = null!;
}