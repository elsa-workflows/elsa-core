using Azure.ResourceManager;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="configureMemberStore">An optional configuration for the member store.</param>
    /// <param name="configure">An optional action to configure the provider options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureContainerAppsProvider(this IServiceCollection services, IArmClientProvider? armClientProvider = default, Action<IServiceCollection>? configureMemberStore = null, Action<AzureContainerAppsProviderOptions>? configure = null)
    {
        var configureOptions = configure ?? (_ => { });
        services.Configure(configureOptions);
        services.ConfigureOptions<AzureContainerAppsProviderOptionsValidator>();
        services.AddSingleton<AzureContainerAppsProvider>();

        if (armClientProvider != null)
            services.AddSingleton(armClientProvider);

        if (configureMemberStore != null)
            configureMemberStore.Invoke(services);
        else
            services.AddResourceTagsMemberStore();

        return services;
    }
}