using System.Text.Json.Serialization;

namespace Elsa.AzureServiceBus.Models;

public record MessageReceivedTriggerPayload
{
    private readonly string _queueOrTopic = default!;
    private readonly string? _subscription;

    public MessageReceivedTriggerPayload(string queueOrTopic, string? subscription)
    {
        QueueOrTopic = queueOrTopic;
        Subscription = subscription;
    }

    [JsonConstructor]
    public MessageReceivedTriggerPayload()
    {
    }

    public string QueueOrTopic
    {
        get => _queueOrTopic;
        init => _queueOrTopic = value.ToLowerInvariant();
    }

    public string? Subscription
    {
        get => _subscription;
        init => _subscription = value?.ToLowerInvariant();
    }

    public void Deconstruct(out string queueOrTopic, out string? subscription)
    {
        queueOrTopic = QueueOrTopic;
        subscription = Subscription;
    }
}