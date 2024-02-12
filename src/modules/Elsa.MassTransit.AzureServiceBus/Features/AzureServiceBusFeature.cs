using Azure.Messaging.ServiceBus.Administration;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.AzureServiceBus.Handlers;
using Elsa.MassTransit.AzureServiceBus.Models;
using Elsa.MassTransit.AzureServiceBus.Options;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
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
    
    /// <summary>
    /// A delegate to create a <see cref="ServiceBusAdministrationClient"/> instance.
    /// </summary>
    public Func<IServiceProvider, ServiceBusAdministrationClient> ServiceBusAdministrationClientFactory { get; set; } = sp => new(GetConnectionString(sp));

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
                RegisterConsumers(consumers);
                
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

    private void RegisterConsumers(List<ConsumerTypeDefinition> consumers)
    {
        var subscriptionTopology = new List<MessageSubscriptionTopology>();
        
        foreach (var consumer in consumers)
        {
            foreach (var consumerInterface in consumer!.ConsumerType.GetInterfaces())
            {
                if (!consumerInterface.IsGenericType ||
                    consumerInterface.GetGenericTypeDefinition() != typeof(IConsumer<>))
                {
                    continue;
                }

                var genericType = consumerInterface.GetGenericArguments()[0];
                var topicName = $"{genericType.Namespace.ToLower()}~{genericType.Name.ToLower()}";
                    
                subscriptionTopology.Add(new MessageSubscriptionTopology(topicName,
                    consumer.Name ?? genericType.Name.ToLower(),
                    consumer.IsShortLived));
            }
        }

        Services.AddSingleton(new MessageTopologyProvider(subscriptionTopology));
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped(ServiceBusAdministrationClientFactory);
        Services.AddNotificationHandler<OrphanedSubscriptionRemover>();
    }

    private static string GetConnectionString(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return configuration.GetConnectionString(options.ConnectionStringOrName) ?? options.ConnectionStringOrName;
    }
}