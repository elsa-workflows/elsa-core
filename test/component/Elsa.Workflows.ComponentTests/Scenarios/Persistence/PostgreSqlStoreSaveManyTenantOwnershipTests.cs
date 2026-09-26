using Elsa.Persistence.EFCore.UnitTests;
using Testcontainers.PostgreSql;

namespace Elsa.Workflows.ComponentTests.Scenarios.Persistence;

[Collection(PostgreSqlStoreSaveManyCollection.Name)]
public sealed class PostgreSqlStoreSaveManyTenantOwnershipTests : StoreSaveManyTenantOwnershipTests
{
    private readonly PostgreSqlStoreSaveManyFixture _fixture;

    public PostgreSqlStoreSaveManyTenantOwnershipTests(PostgreSqlStoreSaveManyFixture fixture)
    {
        _fixture = fixture;
    }

    protected override Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true) =>
        OwnershipStoreScenario.CreatePostgreSqlAsync(_fixture.ConnectionString, tenantId, tenantsEnabled);
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlStoreSaveManyCollection : ICollectionFixture<PostgreSqlStoreSaveManyFixture>
{
    public const string Name = "StoreSaveMany:PostgreSql";
}

public sealed class PostgreSqlStoreSaveManyFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
