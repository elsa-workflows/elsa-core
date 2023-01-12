using System.Text.Json.Serialization;

namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// A bookmark payload model for triggering workflows when messages come in at a given queue or topic and subscription. 
/// </summary>
public record MessageReceivedTriggerPayload
{
    private readonly string _queueOrTopic = default!;
    private readonly string? _subscription;

    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public MessageReceivedTriggerPayload()
    {
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public MessageReceivedTriggerPayload(string queueOrTopic, string? subscription)
    {
        QueueOrTopic = queueOrTopic;
        Subscription = subscription;
    }

    /// <summary>
    /// The queue or topic to trigger from.
    /// </summary>
    public string QueueOrTopic
    {
        get => _queueOrTopic;
        init => _queueOrTopic = value.ToLowerInvariant();
    }

    /// <summary>
    /// The subscription to trigger from.
    /// </summary>
    public string? Subscription
    {
        get => _subscription;
        init => _subscription = value?.ToLowerInvariant();
    }
}