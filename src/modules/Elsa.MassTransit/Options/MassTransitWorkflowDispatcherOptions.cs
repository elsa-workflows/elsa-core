using Elsa.MassTransit.ConsumerDefinitions;

namespace Elsa.MassTransit.Options;

/// Provides options to the <see cref="DispatchWorkflowRequestConsumerDefinition"/>
public class MassTransitWorkflowDispatcherOptions
{
    /// The TTL of queues that are seen as short lived (typically queues that are created per running instance).
    public TimeSpan? ShortTermQueueLifetime { get; set; }
    /// The number of concurrent messages to process.
    public int? ConcurrentMessageLimit { get; set; }
}