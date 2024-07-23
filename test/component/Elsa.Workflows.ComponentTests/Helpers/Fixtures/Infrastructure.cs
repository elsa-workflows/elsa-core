using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Elsa.Workflows.ComponentTests.Helpers;

public class Infrastructure : IAsyncLifetime
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public readonly RabbitMqContainer RabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
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