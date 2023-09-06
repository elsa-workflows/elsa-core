using JetBrains.Annotations;
using StackExchange.Redis;

namespace Proto.Cluster.AzureContainerApps.Stores.Redis;

public class RedisConnectionMultiplexerProvider : IRedisConnectionMultiplexerProvider
{
    private readonly string _connectionString;
    [CanBeNull] private ConnectionMultiplexer _connectionMultiplexer;

    public RedisConnectionMultiplexerProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ConnectionMultiplexer> GetConnectionMultiplexerAsync()
    {
        return _connectionMultiplexer ??= await ConnectionMultiplexer.ConnectAsync(_connectionString);
    }
}