using Azure.ResourceManager;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Proto.Cluster.AzureContainerApps.Actors;
using Proto.Cluster.AzureContainerApps.ClusterProviders;
using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Options;
using Proto.Cluster.AzureContainerApps.Services;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

// ReSharper disable once CheckNamespace
namespace Proto.Cluster.AzureContainerApps;

/// <summary>
/// Adds extension methods to <see cref="IServiceCollection"/> for registering the Azure Container Apps provider
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Container Apps provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the provider to.</param>
    /// <param name="armClientProvider">An <see cref="IArmClientProvider"/> to create <see cref="ArmClient"/> instances.</param>
    /// <param name="configureMemberStore">An optional configuration for the member store.</param>
    /// <param name="configure">An optional action to configure the provider options.</param>
    public static IServiceCollection AddAzureContainerAppsProvider(this IServiceCollection services,
        [CanBeNull] IArmClientProvider armClientProvider = default,
        [CanBeNull] Action<IServiceCollection> configureMemberStore = null,
        [CanBeNull] Action<AzureContainerAppsProviderOptions> configure = null)
    {
        var configureOptions = configure ?? (_ => { });
        services.Configure(configureOptions);
        services.ConfigureOptions<AzureContainerAppsProviderOptionsValidator>();
        services.AddSingleton<AzureContainerAppsProvider>();
        services.AddSingleton<ISystemClock, DefaultSystemClock>();
        services.AddSingleton<IContainerAppMetadataAccessor, EnvironmentContainerAppMetadataAccessor>();
        services.AddTransient<AzureContainerAppsClusterMonitor>();

        if (armClientProvider != null)
            services.AddSingleton(armClientProvider);

        if (configureMemberStore != null)
            // Add the custom member store
            configureMemberStore.Invoke(services);
        else
            // Add the default member store
            services.AddResourceTagsMemberStore();

        return services;
    }
}