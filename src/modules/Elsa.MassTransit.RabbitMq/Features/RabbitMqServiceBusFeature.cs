using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Features;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// A delegate that configures the RabbitMQ transport options.
    /// </summary>
    public Action<RabbitMqTransportOptions>? TransportOptions { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus.
    /// </summary>
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>().BusConfigurator = configure =>
        {
            configure.UsingRabbitMq((context, configurator) =>
            {
                if (!string.IsNullOrEmpty(ConnectionString))
                    configurator.Host(ConnectionString);

                ConfigureServiceBus?.Invoke(configurator);

                configurator.SetupWorkflowDispatcherEndpoints(context);
                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
            });
        };
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (TransportOptions != null) Services.Configure(TransportOptions);
    }
}