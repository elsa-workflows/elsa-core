using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Atomic <see cref="IKeyValueStore.TryDeleteAsync"/> on a migration-built SQLite DB (#260).
/// There is no PostgreSQL Testcontainers setup on this 3.9 branch.
/// </summary>
public sealed class DapperKeyValueStoreTryDeleteTests : IDisposable
{
    private const string LegacyKey = "elsa.quiescence.pause.default";
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-trydelete-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly TestTenantAccessor _tenants = new();

    public DapperKeyValueStoreTryDeleteTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
    }

    [Fact(DisplayName = "#260: two Dapper stores racing TryDeleteAsync: exactly one returns true")]
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

    [Fact(DisplayName = "#260: the default TryDeleteAsync (find then delete) lets both racers win")]
    public async Task DefaultTryDeleteAsync_TwoNodes_BothReturnTrue()
    {
        MigrateUp();
        using var tenant = _tenants.PushContext(Tenant.Default);
        var inner = CreateStore();
        await inner.SaveAsync(Pair(LegacyKey, "legacy-maintenance"), CancellationToken.None);
        IKeyValueStore dim = new BarrierFindKeyValueStore(inner);

        var results = await Task.WhenAll(dim.TryDeleteAsync(LegacyKey), dim.TryDeleteAsync(LegacyKey));

        Assert.Equal(2, results.Count(won => won));
        Assert.Null(await inner.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    [Fact(DisplayName = "#260: TryDeleteAsync returns false when the key is missing")]
    public async Task TryDeleteAsync_NotFound_ReturnsFalse()
    {
        MigrateUp();
        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = CreateStore();

        Assert.False(await store.TryDeleteAsync(LegacyKey));
        Assert.False(await store.TryDeleteAsync(LegacyKey));
    }

    [Fact(DisplayName = "#245 / #260: a NULL TenantId legacy row is found and TryDeleted by the default tenant")]
    public async Task DefaultTenant_FindsAndTryDeletesNullTenantIdRow()
    {
        MigrateUp();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Execute(
                "insert into KeyValues (Id, TenantId, Value) values (@Id, null, @Value)",
                new { Id = LegacyKey, Value = "legacy-null" });
        }

        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = CreateStore();

        var found = await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None);
        Assert.Equal("legacy-null", found?.SerializedValue);
        Assert.True(await store.TryDeleteAsync(LegacyKey));
        Assert.Null(await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    [Fact(DisplayName = "#245 / #260: a named tenant cannot TryDelete a NULL TenantId default-tenant row")]
    public async Task NamedTenant_DoesNotTryDeleteNullTenantIdRow()
    {
        MigrateUp();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Execute(
                "insert into KeyValues (Id, TenantId, Value) values (@Id, null, @Value)",
                new { Id = LegacyKey, Value = "legacy-null" });
        }

        using (var named = _tenants.PushContext(new Tenant { Id = "tenant-a" }))
        {
            var namedStore = CreateStore();
            Assert.Null(await namedStore.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
            Assert.False(await namedStore.TryDeleteAsync(LegacyKey));
        }

        using var tenant = _tenants.PushContext(Tenant.Default);
        Assert.True(TableHasKey(LegacyKey));
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private IKeyValueStore CreateStore()
    {
        var connection = new SqliteDbConnectionProvider(_connectionString);
        return new DapperKeyValueStore(new Store<KeyValuePairRecord>(connection, _tenants, "KeyValues"));
    }

    private void MigrateUp()
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Elsa.Persistence.Dapper.Migrations.Runtime.Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    private bool TableHasKey(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>("select count(*) from KeyValues where Id = @key", new { key }) == 1;
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

    /// <summary>
    /// Forwards Find/Delete and uses the default interface <c>TryDeleteAsync</c>.
    /// Both Finds complete before either Delete so the DIM race is deterministic.
    /// </summary>
    private sealed class BarrierFindKeyValueStore(IKeyValueStore inner) : IKeyValueStore
    {
        private readonly TaskCompletionSource _bothFound = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _finds;

        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken) =>
            inner.SaveAsync(keyValuePair, cancellationToken);

        public async Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
        {
            var found = await inner.FindAsync(filter, cancellationToken);
            if (Interlocked.Increment(ref _finds) == 2)
                _bothFound.TrySetResult();
            await _bothFound.Task;
            return found;
        }

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) =>
            inner.FindManyAsync(filter, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            inner.DeleteAsync(key, cancellationToken);
    }
}
