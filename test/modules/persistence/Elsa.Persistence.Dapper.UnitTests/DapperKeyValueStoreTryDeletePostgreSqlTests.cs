using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Same #260 TryDelete cases as SQLite, on a fresh migration-built PostgreSQL via Testcontainers.
/// Calls the Dapper store method directly; <c>IKeyValueStore.TryDeleteAsync</c> is not
/// in 3.10.0-preview.5722 yet (waiting for a core pin at or after c1c935ce / #8538).
/// </summary>
public sealed class DapperKeyValueStoreTryDeletePostgreSqlTests : IAsyncLifetime
{
    private const string LegacyKey = "elsa.quiescence.pause.default";
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("elsa")
        .WithUsername("elsa")
        .WithPassword("elsa")
        .Build();
    private readonly TestTenantAccessor _tenants = new();
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact(DisplayName = "#260 PG: two Dapper stores racing TryDeleteAsync: exactly one returns true")]
    public async Task TryDeleteAsync_TwoNodes_ExactlyOneReturnsTrue()
    {
        MigrateUp();
        using var tenant = _tenants.PushContext(Tenant.Default);
        var nodeA = CreateStore();
        var nodeB = CreateStore();
        await nodeA.SaveAsync(Pair(LegacyKey, "legacy-maintenance"), CancellationToken.None);

        var results = await Task.WhenAll(nodeA.TryDeleteAsync(LegacyKey), nodeB.TryDeleteAsync(LegacyKey));

        Assert.Equal(1, results.Count(won => won));
        Assert.Equal(1, results.Count(won => !won));
        Assert.Null(await nodeA.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    [Fact(DisplayName = "#260 PG: TryDeleteAsync returns false when the key is missing")]
    public async Task TryDeleteAsync_NotFound_ReturnsFalse()
    {
        MigrateUp();
        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = CreateStore();

        Assert.False(await store.TryDeleteAsync(LegacyKey));
    }

    [Fact(DisplayName = "#245 / #260 PG: a NULL TenantId legacy row is found and TryDeleted by the default tenant")]
    public async Task DefaultTenant_FindsAndTryDeletesNullTenantIdRow()
    {
        MigrateUp();
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Execute(
                """insert into "KeyValues" ("Id", "TenantId", "Value") values (@Id, null, @Value)""",
                new { Id = LegacyKey, Value = "legacy-null" });
        }

        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = CreateStore();

        var found = await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None);
        Assert.Equal("legacy-null", found?.SerializedValue);
        Assert.True(await store.TryDeleteAsync(LegacyKey));
        Assert.Null(await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    private DapperKeyValueStore CreateStore() =>
        new(new Store<KeyValuePairRecord>(new PostgreSqlDbConnectionProvider(_connectionString), _tenants, "KeyValues"));

    private void MigrateUp()
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    private static SerializedKeyValuePair Pair(string key, string value) => new()
    {
        Key = key,
        SerializedValue = value
    };

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
