using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.ConsumerDefinitions;

/// <summary>
/// Configures the endpoint for <see cref="DispatchWorkflowRequestConsumer"/>
/// </summary>
public class DispatchWorkflowRequestConsumerDefinition : ConsumerDefinition<DispatchWorkflowRequestConsumer>
{
    private readonly IOptions<MassTransitWorkflowDispatcherOptions> _options;

    /// <inheritdoc />
    public DispatchWorkflowRequestConsumerDefinition(IOptions<MassTransitWorkflowDispatcherOptions> options)
    {
        _options = options;
        ConcurrentMessageLimit = _options.Value.ConcurrentMessageLimit;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchWorkflowRequestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}