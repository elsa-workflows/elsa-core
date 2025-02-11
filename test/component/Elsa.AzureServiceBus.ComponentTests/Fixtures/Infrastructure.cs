using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Elsa.AzureServiceBus.ComponentTests;

public class Infrastructure : IAsyncLifetime
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
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