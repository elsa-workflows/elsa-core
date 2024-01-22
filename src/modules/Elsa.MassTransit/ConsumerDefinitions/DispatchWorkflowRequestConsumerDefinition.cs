using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Options;

#if NET6_0 || NET7_0
using GreenPipes;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
#endif

namespace Elsa.MassTransit.ConsumerDefinitions;

/// <summary>
/// Configures the endpoint for <see cref="DispatchWorkflowRequestConsumer"/>
/// </summary>
public class DispatchWorkflowRequestConsumerDefinition : ConsumerDefinition<DispatchWorkflowRequestConsumer>
{
    /// <inheritdoc />
    public DispatchWorkflowRequestConsumerDefinition(IOptions<MassTransitWorkflowDispatcherOptions> options)
    {
        ConcurrentMessageLimit = options.Value.ConcurrentMessageLimit;
    }

#if NET6_0 || NET7_0
    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchWorkflowRequestConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.UseInMemoryOutbox();
    }
#endif

#if(NET8_0_OR_GREATER)
    /// <inheritdoc />
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DispatchWorkflowRequestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.UseInMemoryOutbox(context);
    }
#endif
}