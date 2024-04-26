using Testcontainers.PostgreSql;

namespace Elsa.Workflows.ComponentTests;

public class Infrastructure : IAsyncLifetime
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync()
    {
        return DbContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return DbContainer.StopAsync();
    }
}