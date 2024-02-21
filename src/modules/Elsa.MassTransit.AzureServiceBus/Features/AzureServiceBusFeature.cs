using Azure.Messaging.ServiceBus.Administration;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.MassTransit.AzureServiceBus.Handlers;
using Elsa.MassTransit.AzureServiceBus.Models;
using Elsa.MassTransit.AzureServiceBus.Options;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Options;
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
    /// A delegate to configure <see cref="AzureServiceBusOptions"/>.
    /// </summary>
    public Action<AzureServiceBusOptions> AzureServiceBusOptions { get; set; } = _ => { };

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
                var temporaryConsumers = consumers
                    .Where(c => c.IsTemporary)
                    .ToList();
                RegisterConsumers(consumers);

                configure.AddServiceBusMessageScheduler();
                
                configure.AddConsumers(temporaryConsumers.Select(c => c.ConsumerType).ToArray());

                configure.UsingAzureServiceBus((context, configurator) =>
                {
                    if (ConnectionString != null) 
                        configurator.Host(ConnectionString);
                    
                    configurator.UseServiceBusMessageScheduler();
                    configurator.SetupWorkflowDispatcherEndpoints(context);
                    ConfigureServiceBus?.Invoke(configurator);
                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                    var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;
                    var instanceNameProvider = context.GetRequiredService<IApplicationInstanceNameProvider>();


                    foreach (var consumer in temporaryConsumers)
                    {
                        configurator.ReceiveEndpoint($"Elsa-{instanceNameProvider.GetName()}-{consumer.Name}", configurator =>
                        {
                            configurator.AutoDeleteOnIdle = options.TemporaryQueueTtl ?? TimeSpan.FromHours(1);
                            configurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                            configurator.ConfigureConsumer(context, consumer.ConsumerType);
                        });
                    }

                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }

    private void RegisterConsumers(List<ConsumerTypeDefinition> consumers)
    {
        var subscriptionTopology = (
            from consumer in consumers
            from consumerInterface in consumer.ConsumerType.GetInterfaces()
            where consumerInterface.IsGenericType && consumerInterface.GetGenericTypeDefinition() == typeof(IConsumer<>)
            let genericType = consumerInterface.GetGenericArguments()[0]
            let topicName = $"{genericType.Namespace.ToLower()}/{genericType.Name.ToLower()}"
            select new MessageSubscriptionTopology(topicName, consumer.Name ?? genericType.Name.ToLower(), consumer.IsTemporary)
        ).ToList();

        Services.AddSingleton(new MessageTopologyProvider(subscriptionTopology));
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(AzureServiceBusOptions);
        Services.AddSingleton(ServiceBusAdministrationClientFactory);
        Services.AddNotificationHandler<RemoveOrphanedSubscriptions>();
    }

    private static string GetConnectionString(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return configuration.GetConnectionString(options.ConnectionStringOrName) ?? options.ConnectionStringOrName;
    }
}