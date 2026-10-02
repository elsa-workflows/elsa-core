using Elsa.Expressions.Models;

namespace Elsa.ServiceBus.Kafka.Stimuli;

public class MessageReceivedStimulus
{
    public string ConsumerDefinitionId { get; set; } = null!;
    public ICollection<string> Topics { get; set; } = [];
    public Expression? Predicate { get; set; }
    public bool IsLocal { get; set; }

    /// <summary>
    /// Optional. When set, only messages whose deserialized Avro schema full name matches this value will be processed.
    /// Requires the consumer to use <see cref="Factories.AvroConsumerFactory"/> or another factory that produces <see cref="Avro.Generic.GenericRecord"/> values.
    /// </summary>
    public string? SchemaFullName { get; set; }
}
