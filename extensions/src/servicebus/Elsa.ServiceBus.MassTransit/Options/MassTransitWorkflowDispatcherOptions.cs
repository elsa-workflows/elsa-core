using Elsa.ServiceBus.MassTransit.ConsumerDefinitions;

namespace Elsa.ServiceBus.MassTransit.Options;

/// <summary>
/// Provides options to the <see cref="DispatchWorkflowRequestConsumerDefinition"/>
/// </summary>
public class MassTransitWorkflowDispatcherOptions
{
    /// <summary>
    /// The number of concurrent messages to process.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }
}