using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    /// Configures the RabbitMQ transport options.
    public Action<RabbitMqTransportOptions>? TransportOptions { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus.
    /// </summary>
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                var tempConsumers = massTransitFeature.GetConsumers()
                    .Where(c => c.IsTemporary)
                    .ToList();

                configure.AddConsumers(tempConsumers.Select(c => c.ConsumerType).ToArray());

                configure.UsingRabbitMq((context, configurator) =>
                {
                    var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;
                    var instanceNameProvider = context.GetRequiredService<IApplicationInstanceNameProvider>();

                    if (!string.IsNullOrEmpty(ConnectionString))
                        configurator.Host(ConnectionString);

                    ConfigureServiceBus?.Invoke(configurator);

                    foreach (var consumer in tempConsumers)
                    {
                        configurator.ReceiveEndpoint($"{instanceNameProvider.GetName()}-{consumer.Name}",
                            configurator =>
                            {
                                configurator.QueueExpiration = options.TemporaryQueueTtl ?? TimeSpan.FromHours(1);
                                configurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                                configurator.ConfigureConsumer<DispatchCancelWorkflowsRequestConsumer>(context);
                            });
                    }

                    configurator.SetupWorkflowDispatcherEndpoints(context);
                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (TransportOptions != null) Services.Configure(TransportOptions);
    }
}