using System;
using System.Diagnostics.CodeAnalysis;
using Azure.ResourceManager;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

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
    /// <param name="configure">An optional action to configure the provider options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureContainerAppsProvider(this IServiceCollection services, IArmClientProvider? armClientProvider = default, [AllowNull] Action<AzureContainerAppsProviderOptions> configure = null)
    {
        var configureOptions = configure ?? (_ => { });
        services.Configure(configureOptions);
        services.ConfigureOptions<AzureContainerAppsProviderOptionsValidator>();
        services.AddSingleton<AzureContainerAppsProvider>();

        if (armClientProvider != null)
            services.AddSingleton(armClientProvider);

        // Register the default member store.
        services.AddSingleton<IClusterMemberStore, ResourceTagsClusterMemberStore>(sp =>
        {
            var clientProvider = sp.GetRequiredService<IArmClientProvider>();
            var logger = sp.GetRequiredService<ILogger<ResourceTagsClusterMemberStore>>();
            var options = sp.GetRequiredService<IOptions<AzureContainerAppsProviderOptions>>().Value;
            return new ResourceTagsClusterMemberStore(clientProvider, logger, options.ResourceGroupName, options.SubscriptionId);
        });

        return services;
    }
}