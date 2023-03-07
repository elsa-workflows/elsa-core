using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Handlers;
using Elsa.AzureServiceBus.HostedServices;
using Elsa.AzureServiceBus.Options;
using Elsa.AzureServiceBus.Providers;
using Elsa.AzureServiceBus.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.AzureServiceBus.Features;

/// <summary>
/// Enables and configures the Azure Service Bus feature.
/// </summary>
public class AzureServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public AzureServiceBusFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A value controlling whether or not queues, topics and subscriptions should be created automatically. 
    /// </summary>
    public bool CreateQueuesTopicsAndSubscriptions { get; set; } = true;

    /// <summary>
    /// A delegate to configure <see cref="AzureServiceBusOptions"/>.
    /// </summary>
    public Action<AzureServiceBusOptions> AzureServiceBusOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        if (CreateQueuesTopicsAndSubscriptions)
            Module.ConfigureHostedService<CreateQueuesTopicsAndSubscriptions>();

        Module.ConfigureHostedService<StartWorkers>();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Activities.
        Module.AddActivitiesFrom<AzureServiceBusFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(AzureServiceBusOptions);

        Services
            .AddSingleton(CreateServiceBusManagementClient)
            .AddSingleton(CreateServiceBusClient)
            .AddSingleton<ConfigurationQueueTopicAndSubscriptionProvider>()
            .AddSingleton<IWorkerManager, WorkerManager>()
            .AddTransient<IServiceBusInitializer, ServiceBusInitializer>();

        // Definition providers.
        Services
            .AddSingleton<IQueueProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>())
            .AddSingleton<ITopicProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>())
            .AddSingleton<ISubscriptionProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>());

        // Handlers.
        Services.AddHandlersFrom<UpdateWorkers>();
    }
    
    private static ServiceBusClient CreateServiceBusClient(IServiceProvider serviceProvider) => new(GetConnectionString(serviceProvider));
    private static ServiceBusAdministrationClient CreateServiceBusManagementClient(IServiceProvider serviceProvider) => new(GetConnectionString(serviceProvider));

    private static string GetConnectionString(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return configuration.GetConnectionString(options.ConnectionStringOrName) ?? options.ConnectionStringOrName;
    }
}