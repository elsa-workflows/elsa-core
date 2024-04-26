using Testcontainers.PostgreSql;

namespace Elsa.Workflows.ComponentTests;

public class Infrastructure : IAsyncLifetime
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:13.3-alpine")
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