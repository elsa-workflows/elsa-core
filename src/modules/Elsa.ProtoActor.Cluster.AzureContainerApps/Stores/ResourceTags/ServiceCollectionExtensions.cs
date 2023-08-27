using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proto.Cluster.AzureContainerApps.Contracts;
using Proto.Cluster.AzureContainerApps.Options;

namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Adds extension methods to <see cref="IServiceCollection"/> for registering the Azure Container Apps provider
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="ResourceTagsClusterMemberStore"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the provider to.</param>
    /// <param name="configure">An optional action to configure the provider options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddResourceTagsMemberStore(this IServiceCollection services, [AllowNull] Action<ResourceTagsMemberStoreOptions> configure = null)
    {
        var configureOptions = configure ?? (_ => { });
        services.Configure(configureOptions);
        services.ConfigureOptions<ResourceTagsMemberStoreOptionsValidator>();

        services.AddSingleton<IClusterMemberStore, ResourceTagsClusterMemberStore>(sp =>
        {
            var clientProvider = sp.GetRequiredService<IArmClientProvider>();
            var logger = sp.GetRequiredService<ILogger<ResourceTagsClusterMemberStore>>();
            var systemClock = sp.GetRequiredService<ISystemClock>();
            var options = sp.GetRequiredService<IOptions<AzureContainerAppsProviderOptions>>().Value;
            return new ResourceTagsClusterMemberStore(clientProvider, systemClock, logger, options.ResourceGroupName, options.SubscriptionId);
        });

        return services;
    }
}