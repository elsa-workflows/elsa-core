using Confluent.SchemaRegistry;
using Elsa.Common.Entities;

namespace Elsa.ServiceBus.Kafka;

public class SchemaRegistryDefinition : Entity
{
    public string Name { get; set; } = null!;
    public SchemaRegistryConfig Config { get; set; } = null!;
}