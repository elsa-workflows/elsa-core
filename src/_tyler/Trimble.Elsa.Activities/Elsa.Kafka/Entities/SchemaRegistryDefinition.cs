using Confluent.SchemaRegistry;
using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class SchemaRegistryDefinition : Entity
{
    public string Name { get; set; } = default!;
    public SchemaRegistryConfig Config { get; set; } = default!;
}