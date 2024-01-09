using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Options;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.ConsumerDefinitions;

/// <summary>
/// Configures the endpoint for <see cref="DispatchCancelWorkflowsRequestConsumer"/>
/// </summary>
public class DispatchCancelWorkflowsRequestConsumerDefinition : ConsumerDefinition<DispatchCancelWorkflowsRequestConsumer>
{
    private readonly IOptions<MassTransitWorkflowDispatcherOptions> _options;

    /// <inheritdoc />
    public DispatchCancelWorkflowsRequestConsumerDefinition(IOptions<MassTransitWorkflowDispatcherOptions> options)
    {
        _options = options;
        ConcurrentMessageLimit = _options.Value.ConcurrentMessageLimit;
        EndpointName = "random-queue-name" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.EndpointDefinition!.IsTemporary = true;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchCancelWorkflowsRequestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.tem
    }
}