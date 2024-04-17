namespace Elsa.MassTransit.Options;

/// <summary>
/// Represents the options for configuring MassTransit.
/// </summary>
public class MassTransitOptions
{
    /// The TTL of queues that are seen as temporary (typically queues that are created per running instance).
    public TimeSpan? TemporaryQueueTtl { get; set; }
    /// The number of concurrent messages to process.
    public int? ConcurrentMessageLimit { get; set; }
    /// The number of messages to fetch from the bus with each request.
    public int? PrefetchCount { get; set; }
    /// The maximum duration for auto-renewal of a resource.
    /// <remarks>Only relevant when using Azure Service Bus.</remarks>
    public TimeSpan? MaxAutoRenewDuration { get; set; }
}