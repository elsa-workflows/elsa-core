namespace Elsa.MassTransit.Options;

/// <summary>
/// Represents the options for configuring MassTransit.
/// </summary>
public class MassTransitOptions
{
    /// <summary>
    /// The TTL of queues that are seen as temporary (typically queues that are created per running instance).
    /// </summary>
    public TimeSpan? TemporaryQueueTtl { get; set; }

    /// <summary>
    /// The number of concurrent messages to process.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>
    /// The number of messages to fetch from the bus with each request.
    /// </summary>
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// The maximum duration for auto-renewal of a resource.
    /// </summary>
    /// <remarks>Only relevant when using Azure Service Bus.</remarks>
    public TimeSpan? MaxAutoRenewDuration { get; set; }
}