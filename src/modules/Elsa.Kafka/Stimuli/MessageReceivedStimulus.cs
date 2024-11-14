using Elsa.Expressions.Models;
using Elsa.Workflows.Attributes;

namespace Elsa.Kafka.Stimuli;

public class MessageReceivedStimulus
{
    public string ConsumerDefinitionId { get; set; } = default!;
    [ExcludeFromHash] public Type? MessageType { get; set; }
    public ICollection<string> Topics { get; set; } = [];
    public IDictionary<string, object?>? CorrelatingFields { get; set; }
    public Expression? Predicate { get; set; }
}