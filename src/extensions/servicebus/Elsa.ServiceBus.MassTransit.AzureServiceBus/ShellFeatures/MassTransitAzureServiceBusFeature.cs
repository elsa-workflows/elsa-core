using Azure.Messaging.ServiceBus.Administration;
using CShells.Configuration;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Configurators;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Handlers;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.HostedServices;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Options;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Services;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.ShellFeatures;

/// <summary>
/// Configures MassTransit to use Azure Service Bus as the transport.
/// </summary>
/// <remarks>
/// Registers an <see cref="AzureServiceBusTransportConfigurator"/> as the
/// <see cref="IBusTransportConfigurator"/> singleton, replacing the default in-memory
/// configurator registered by <see cref="MassTransitFeature"/>.
/// <see cref="MassTransitFeature.PostConfigureServices"/> then calls
/// <c>AddMassTransit</c> exactly once, picking up this configurator.
/// </remarks>
[ShellFeature(
    DisplayName = "MassTransit Azure Service Bus Transport",
    Description = "Configures MassTransit to use Azure Service Bus as the message transport",
    DependsOn = [typeof(MassTransitFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("azure-service-bus", "service-bus", Reason = "Configures MassTransit to use Azure Service Bus as its transport.", Providers = new[] { "Azure Service Bus" }, ConfigurationKeys = new[] { "MassTransitAzureServiceBus:ConnectionStringOrName", "MassTransitAzureServiceBusCleanup" })]
public class MassTransitAzureServiceBusFeature : IShellFeature
{
    /// <summary>
    /// When <c>true</c>, registers a hosted service that periodically removes orphaned
    /// subscriptions whose connected queues no longer exist.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Enable automated subscription cleanup",
        Description = "Periodically removes orphaned Azure Service Bus subscriptions whose connected queues no longer exist.",
        Category = "Cleanup",
        Advanced = true,
        RestartRequired = true)]
    public bool EnableAutomatedSubscriptionCleanup { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Bind AzureServiceBusOptions from this feature's own configuration section.
        services.AddOptions<AzureServiceBusOptions>()
            .Configure<ShellConfiguration>((options, config) =>
                config.GetSection("MassTransitAzureServiceBus").Bind(options));

        services.AddOptions<SubscriptionCleanupOptions>()
            .Configure<ShellConfiguration>((options, config) =>
                config.GetSection("MassTransitAzureServiceBusCleanup").Bind(options));

        // ServiceBusAdministrationClient — resolved from the connection string at runtime.
        services.AddSingleton<ServiceBusAdministrationClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ConnectionStringOrName))
                throw new InvalidOperationException("Azure Service Bus connection string or name is not configured.");

            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(options.ConnectionStringOrName) ?? options.ConnectionStringOrName;
            return new(connectionString);
        });

        services.AddSingleton<MessageTopologyProvider>();
        services.AddNotificationHandler<RemoveOrphanedSubscriptions>();
        services.AddCommandHandler<CleanupOrphanedTopology>();

        if (EnableAutomatedSubscriptionCleanup)
            services.AddHostedService<CleanSubscriptionsWithoutQueues>();

        // Replace the default InMemoryTransportConfigurator.
        services.AddSingleton<IBusTransportConfigurator, AzureServiceBusTransportConfigurator>();
    }
}
