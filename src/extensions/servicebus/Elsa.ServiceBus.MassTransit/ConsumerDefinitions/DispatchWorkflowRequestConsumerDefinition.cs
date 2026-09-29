using Elsa.ServiceBus.MassTransit.Consumers;
using Elsa.ServiceBus.MassTransit.Options;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.ConsumerDefinitions;

/// <summary>
/// Configures the endpoint for <see cref="DispatchWorkflowRequestConsumer"/>
/// </summary>
[UsedImplicitly]
public class DispatchWorkflowRequestConsumerDefinition : ConsumerDefinition<DispatchWorkflowRequestConsumer>
{
    /// <inheritdoc />
    public DispatchWorkflowRequestConsumerDefinition(IOptions<MassTransitWorkflowDispatcherOptions> options)
    {
        ConcurrentMessageLimit = options.Value.ConcurrentMessageLimit;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchWorkflowRequestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}