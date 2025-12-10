using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Elsa.Workflows.ComponentTests.Fixtures;

public class Infrastructure : IAsyncLifetime
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCommand(
            "-c", "max_connections=100",
            "-c", "shared_buffers=128MB",
            "-c", "work_mem=4MB",
            "-c", "effective_cache_size=256MB"
        )
        .Build();

    public readonly RabbitMqContainer RabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management")
        .Build();

    public Task InitializeAsync()
    {
        return Task.WhenAll(
            DbContainer.StartAsync(),
            RabbitMqContainer.StartAsync());
    }

    public Task DisposeAsync()
    {
        return Task.WhenAll(
            DbContainer.StopAsync(),
            RabbitMqContainer.StopAsync());
    }
}