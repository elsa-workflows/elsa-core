using Elsa.MassTransit.ConsumerDefinitions;

namespace Elsa.MassTransit.Options;

/// <summary>
/// Provides options to the <see cref="DispatchStimulusRequestConsumerDefinition"/>
/// </summary>
public class MassTransitStimulusDispatcherOptions
{
    /// <summary>
    /// The number of concurrent messages to process.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }
}