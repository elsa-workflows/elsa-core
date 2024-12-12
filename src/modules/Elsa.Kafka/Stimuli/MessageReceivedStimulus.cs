using Elsa.Expressions.Models;

namespace Elsa.Kafka.Stimuli;

public class MessageReceivedStimulus
{
    public string ConsumerDefinitionId { get; set; } = default!;
    public ICollection<string> Topics { get; set; } = [];
    public Expression? Predicate { get; set; }
    public bool IsLocal { get; set; }
}