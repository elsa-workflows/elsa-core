using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.AzureServiceBus.Handlers;
using Elsa.AzureServiceBus.HostedServices;
using Elsa.AzureServiceBus.Implementations;
using Elsa.AzureServiceBus.Options;
using Elsa.AzureServiceBus.Providers;
using Elsa.AzureServiceBus.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Register required services for the Azure Service Bus module. 
    /// </summary>
    /// <param name="autoCreateQueuesTopicsAndSubscriptions">A value indicating whether or not queues, topics and subscriptions should be created at application startup.</param>
    public static IServiceCollection AddAzureServiceBusServices(this IServiceCollection services, Action<AzureServiceBusOptions> configure, bool autoCreateQueuesTopicsAndSubscriptions = true)
    {
        services.Configure(configure);

        services
            .AddSingleton(CreateServiceBusManagementClient)
            .AddSingleton(CreateServiceBusClient)
            .AddSingleton<ConfigurationQueueTopicAndSubscriptionProvider>()
            .AddSingleton<IWorkerManager, WorkerManager>()
            .AddTransient<IServiceBusInitializer, ServiceBusInitializer>();

        // Definition providers.
        services
            .AddSingleton<IQueueProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>())
            .AddSingleton<ITopicProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>())
            .AddSingleton<ISubscriptionProvider>(sp => sp.GetRequiredService<ConfigurationQueueTopicAndSubscriptionProvider>());

        // Hosted services.
        if (autoCreateQueuesTopicsAndSubscriptions)
            services.AddHostedService<CreateQueuesTopicsAndSubscriptions>();

        // Handlers.
        services.AddHandlersFrom<UpdateWorkers>();

        return services;
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