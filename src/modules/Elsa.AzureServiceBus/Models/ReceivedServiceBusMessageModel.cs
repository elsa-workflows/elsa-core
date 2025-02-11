using JetBrains.Annotations;

namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// A serializable version of <see cref="Azure.Messaging.ServiceBus.ServiceBusReceivedMessage"/>.
/// </summary>
// Needs to be a class and not a record, because of the polymorphic serialization that cannot deal with $type properties.
[PublicAPI]
public class ReceivedServiceBusMessageModel
{
    public byte[] Body { get; init; } = default!;
    public string? Subject { get; init; }
    public string? ContentType { get; init; }
    public string? To { get; init; }
    public string? CorrelationId { get; init; }
    public int DeliveryCount { get; init; }
    public DateTimeOffset EnqueuedTime { get; init; }
    public DateTimeOffset ScheduledEnqueuedTime { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset LockedUntil { get; init; }
    public TimeSpan TimeToLive { get; init; }
    public string? LockToken { get; init; }
    public string? MessageId { get; init; }
    public string? PartitionKey { get; init; }
    public string? TransactionPartitionKey { get; init; }
    public string? ReplyTo { get; init; }
    public long SequenceNumber { get; init; }
    public long EnqueuedSequenceNumber { get; init; }
    public string? SessionId { get; init; }
    public string? ReplyToSessionId { get; init; }
    public string? DeadLetterReason { get; init; }
    public string? DeadLetterSource { get; init; }
    public string? DeadLetterErrorDescription { get; init; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; init; } = new Dictionary<string, object>();
}