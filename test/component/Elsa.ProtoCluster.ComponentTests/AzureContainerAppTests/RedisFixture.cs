using Testcontainers.Redis;

namespace Elsa.ProtoCluster.ComponentTests.AzureContainerAppTests;

public class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _redisContainer;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder()
            .WithPortBinding(6379, true)
            .Build();

        await _redisContainer.StartAsync().ConfigureAwait(false);
    }
    
    public string GetConnectionString()
    {
        return _redisContainer != null ? 
            _redisContainer.GetConnectionString() : 
            throw new InvalidOperationException("Redis container not initialized.");
    }

    public async Task DisposeAsync()
    {
        if(_redisContainer != null)
            await _redisContainer.StopAsync();
    }
}
