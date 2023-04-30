using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public static IServiceCollection AddResourceTagsMemberStore(this IServiceCollection services, [AllowNull]Action<ResourceTagsMemberStoreOptions> configure = null)
    {
        var configureOptions = configure ?? (_ => { });
        services.Configure(configureOptions);
        services.ConfigureOptions<ResourceTagsMemberStoreOptionsValidator>();
        services.Replace(new ServiceDescriptor(typeof(IClusterMemberStore), typeof(ResourceTagsClusterMemberStore), ServiceLifetime.Singleton));

        return services;
    }
}