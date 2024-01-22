using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Features;
using MassTransit;
using MassTransit.RabbitMqTransport;

#if NET6_0 || NET7_0
using MassTransit.Definition;
#endif

namespace Elsa.MassTransit.RabbitMq.Features;

/// <summary>
/// Configures MassTransit to use the RabbitMQ transport.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    /// A RabbitMQ connection string.
    public string? ConnectionString { get; set; }

#if NET_80_OR_GREATER
    /// Configures the RabbitMQ transport options.
    public Action<RabbitMqTransportOptions>? TransportOptions { get; set; }
#endif

    /// <summary>
    /// Configures the RabbitMQ bus.
    /// </summary>
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, serviceBus) =>
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    serviceBus.Host(ConnectionString);

                ConfigureServiceBus?.Invoke(serviceBus);

                serviceBus.ReceiveEndpoint("elsa-dispatch-workflow-request", endpoint =>
                {
                    endpoint.ConfigureConsumer<DispatchWorkflowRequestConsumer>(context);
                });

                serviceBus.ReceiveEndpoint("elsa-dispatch-workflow-request-channel-1", endpoint =>
                {
                    endpoint.ConfigureConsumer<DispatchWorkflowRequestConsumer>(context);
                });
                serviceBus.ReceiveEndpoint("elsa-dispatch-workflow-request-channel-2", endpoint => { });
                serviceBus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                
            });
        };
    }

    /// <inheritdoc />
    public override void Apply()
    {
#if NET_80_OR_GREATER
        if (TransportOptions != null) Services.Configure(TransportOptions);
#endif
    }
}