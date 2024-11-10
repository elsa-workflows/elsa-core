using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class ProducerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public ICollection<string> BootstrapServers { get; set; } = [];
}