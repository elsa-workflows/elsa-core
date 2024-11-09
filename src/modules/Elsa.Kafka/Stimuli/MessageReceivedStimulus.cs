using System.Text.Json.Serialization;

namespace Elsa.Kafka.Stimuli;

public class MessageReceivedStimulus
{
    [JsonConstructor]
    public MessageReceivedStimulus()
    {
    }

    public MessageReceivedStimulus(string consumerDefinitionId)
    {
        ConsumerDefinitionId = consumerDefinitionId;
    }

    public string ConsumerDefinitionId { get; set; } = default!;
}