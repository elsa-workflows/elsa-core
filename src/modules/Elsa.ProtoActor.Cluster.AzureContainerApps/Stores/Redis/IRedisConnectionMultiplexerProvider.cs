using StackExchange.Redis;

namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

public interface IRedisConnectionMultiplexerProvider
{
    Task<ConnectionMultiplexer> GetConnectionMultiplexerAsync();
}