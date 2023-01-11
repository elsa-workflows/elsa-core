using JetBrains.Annotations;

namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// A serializable version of <see cref="Azure.Messaging.ServiceBus.ServiceBusReceivedMessage"/>.
/// </summary>
[PublicAPI]
public record ReceivedServiceBusMessageModel(
    byte[] Body,
    string Subject,
    string ContentType,
    string To,
    string CorrelationId,
    int DeliveryCount,
    DateTimeOffset EnqueuedTime,
    DateTimeOffset ScheduledEnqueuedTime,
    DateTimeOffset ExpiresAt,
    DateTimeOffset LockedUntil,
    TimeSpan TimeToLive,
    string LockToken,
    string MessageId,
    string PartitionKey,
    string TransactionPartitionKey,
    string ReplyTo,
    long SequenceNumber,
    long EnqueuedSequenceNumber,
    string SessionId,
    string ReplyToSessionId,
    string DeadLetterReason,
    string DeadLetterSource,
    string DeadLetterErrorDescription,
    IReadOnlyDictionary<string, object> ApplicationProperties);