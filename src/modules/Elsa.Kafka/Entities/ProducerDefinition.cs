using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class ProducerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public ProducerConfig Config { get; set; } = new();
}