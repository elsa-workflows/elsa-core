using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.Kafka;

public class ConsumerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public ICollection<string> BootstrapServers { get; set; } = [];
    public string GroupId { get; set; } = default!;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public bool EnableAutoCommit { get; set; }
}