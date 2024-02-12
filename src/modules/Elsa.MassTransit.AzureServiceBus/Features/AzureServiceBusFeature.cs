using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.AzureServiceBus.Features;

/// Configures MassTransit to use the Azure Service Bus transport.
/// See https://masstransit.io/documentation/configuration/transports/azure-service-bus
[DependsOn(typeof(MassTransitFeature))]
public class AzureServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public AzureServiceBusFeature(IModule module) : base(module)
    {
    }
    
    /// An Azure Service Bus connection string.
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A delegate that configures the Azure Service Bus transport options.
    /// </summary>
    public Action<IServiceBusBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                var consumers = massTransitFeature.GetConsumers().ToList();
                var shortLivedConsumers = consumers
                    .Where(c => c.IsShortLived)
                    .ToList();
                
                configure.AddServiceBusMessageScheduler();
                configure.AddConsumers(shortLivedConsumers.Select(c => c.ConsumerType).ToArray());
                
                configure.UsingAzureServiceBus((context, serviceBus) =>
                {
                    var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;
                    var instanceNameRetriever = context.GetRequiredService<IInstanceNameRetriever>();

                    if (ConnectionString != null) 
                        serviceBus.Host(ConnectionString);
                    serviceBus.UseServiceBusMessageScheduler();
                    ConfigureServiceBus?.Invoke(serviceBus);

                    foreach (var consumer in shortLivedConsumers)
                    {
                        serviceBus.ReceiveEndpoint($"Elsa-{instanceNameRetriever.GetName()}-{consumer.Name}", configurator =>
                        {
                            configurator.AutoDeleteOnIdle = options.ShortTermQueueLifetime ?? TimeSpan.FromHours(1);
                            configurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                            configurator.ConfigureConsumer(context, consumer.ConsumerType);
                        });
                    }

                    serviceBus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }
}