using System.Text.Json.Serialization;

namespace Elsa.Kafka.Stimuli;

public class MessageReceivedStimulus
{
    [JsonConstructor]
    public MessageReceivedStimulus()
    {
    }

    public MessageReceivedStimulus(string consumerDefinitionId, IDictionary<string, object?> correlatingFields)
    {
        ConsumerDefinitionId = consumerDefinitionId;
        CorrelatingFields = correlatingFields;
    }

    public string ConsumerDefinitionId { get; set; } = default!;
    public IDictionary<string, object?> CorrelatingFields { get; set; } = new Dictionary<string, object?>();
}