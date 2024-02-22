using Elsa.MassTransit.ConsumerDefinitions;

namespace Elsa.MassTransit.Options;

/// Provides options to the <see cref="DispatchWorkflowRequestConsumerDefinition"/>
public class MassTransitWorkflowDispatcherOptions
{
    /// The TTL of queues that are seen as temporary (typically queues that are created per running instance).
    public TimeSpan? TemporaryQueueTtl { get; set; }
    /// The number of concurrent messages to process.
    public int? ConcurrentMessageLimit { get; set; }
}