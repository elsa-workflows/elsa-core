using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Options;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.ConsumerDefinitions;

/// <summary>
/// Configures the endpoint for <see cref="DispatchStimulusRequestConsumer"/>
/// </summary>
[UsedImplicitly]
public class DispatchStimulusRequestConsumerDefinition : ConsumerDefinition<DispatchStimulusRequestConsumer>
{
    /// <inheritdoc />
    public DispatchStimulusRequestConsumerDefinition(IOptions<MassTransitWorkflowDispatcherOptions> options)
    {
        ConcurrentMessageLimit = options.Value.ConcurrentMessageLimit;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchStimulusRequestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}