using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Elsa.Workflows.ComponentTests.Fixtures;

public class Infrastructure : IAsyncLifetime
{
    //public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder().Build();

    public readonly MsSqlContainer DbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU13-ubuntu-22.04")
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