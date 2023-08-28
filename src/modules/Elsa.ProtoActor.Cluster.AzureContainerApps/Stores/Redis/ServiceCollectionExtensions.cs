using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Proto.Cluster.AzureContainerApps.Contracts;

namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

/// <summary>
/// Adds extension methods to <see cref="IServiceCollection"/> for registering the Azure Container Apps provider
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="RedisClusterMemberStore"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the provider to.</param>
    /// <param name="connectionString">Connection string for the Redis client.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRedisClusterMemberStore(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IRedisConnectionMultiplexerProvider>(new RedisConnectionMultiplexerProvider(connectionString));
        services.AddSingleton<IClusterMemberStore, RedisClusterMemberStore>();
        return services;
    }
}