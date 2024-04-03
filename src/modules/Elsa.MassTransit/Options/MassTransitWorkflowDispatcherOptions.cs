using Elsa.MassTransit.ConsumerDefinitions;

namespace Elsa.MassTransit.Options;

/// Provides options to the <see cref="DispatchWorkflowRequestConsumerDefinition"/>
public class MassTransitWorkflowDispatcherOptions
{
    /// The number of concurrent messages to process.
    public int? ConcurrentMessageLimit { get; set; }
}