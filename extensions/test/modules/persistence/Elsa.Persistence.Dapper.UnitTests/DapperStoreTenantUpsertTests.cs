using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-scoped Dapper <see cref="Store{T}"/> upserts (#9010). Defaults to SQLite; set
/// <c>ELSA_DAPPER_CAS_PROVIDER</c> / <c>ELSA_DAPPER_CAS_CONNECTION</c> to run on PostgreSQL or SQL Server.
/// </summary>
public sealed class DapperStoreTenantUpsertTests : IDisposable
{
    private readonly string _backend = Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_PROVIDER") ?? "sqlite";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"elsa-dapper-store-upsert-{Guid.NewGuid():N}.db");
    private readonly string _table = $"Items_{Guid.NewGuid():N}";
    private readonly IDbConnectionProvider _provider;
    private readonly Store<ItemRecord> _store;
    private readonly RoleTestTenantAccessor _tenantAccessor = new();

    private string Q(string identifier) => _backend == "postgres" ? $"\"{identifier}\"" : identifier;

    public DapperStoreTenantUpsertTests()
    {
        _provider = _backend switch
        {
            "sqlite" => new SqliteDbConnectionProvider(new SqliteConnectionStringBuilder
            {
                DataSource = _path, Pooling = false, DefaultTimeout = 1
            }.ToString()),
            "postgres" => new PostgreSqlDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            "sqlserver" => new SqlServerDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            _ => throw new InvalidOperationException("Unsupported proof provider")
        };

        using var connection = _provider.GetConnection();
        var schema = $"create table {Q(_table)} ({Q("Id")} text not null primary key, {Q("TenantId")} text null, {Q("Name")} text not null)";
        if (_backend == "sqlserver")
            schema = $"create table {Q(_table)} ({Q("Id")} varchar(255) not null primary key, {Q("TenantId")} nvarchar(255) null, {Q("Name")} nvarchar(max) not null)";
        connection.Execute(schema);

        _store = new Store<ItemRecord>(_provider, _tenantAccessor, _table);
    }

    [Fact]
    public async Task CrossTenantIdCollision_Throws_AndLeavesTenantARowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Item("shared", "owned-by-a"));

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "taken-by-b")));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    [Fact]
    public async Task SameTenantUpsert_UpdatesTheExistingRow()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "original"));
            await _store.SaveAsync(Item("shared", "updated"));
        }

        Assert.Equal([("shared", "tenant-a", "updated")], LoadItems());
    }

    [Fact]
    public async Task SaveManyAsync_CrossTenantClash_Throws_AndLeavesTenantARowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Item("clash", "owned-by-a"));

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveManyAsync([Item("fresh", "from-b"), Item("clash", "taken-by-b")]));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Contains(("clash", "tenant-a", "owned-by-a"), LoadItems());
    }

    [Fact]
    public async Task DefaultTenant_UpdatesLegacyNullTenantRow_AndDoesNotThrow()
    {
        Seed("legacy", tenantId: null, name: "original");

        using (_tenantAccessor.PushContext(Tenant.Default))
            await _store.SaveAsync(Item("legacy", "updated"));

        var row = Assert.Single(LoadItems());
        Assert.Equal("legacy", row.Id);
        Assert.Equal("updated", row.Name);
        Assert.True(string.IsNullOrEmpty(row.TenantId));
    }

    // Closest store-level equivalent of "multi-tenant startup loads a '*' definition for every
    // tenant with no error": a save against an existing '*' row does not throw. Loading that
    // row (and its triggers) for every tenant needs '*' visibility on reads, which stays in #9010.
    [Fact]
    public async Task ExistingAgnosticRow_SavedByConcreteTenant_OverwritesLikeMain()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");

        using (_tenantAccessor.PushContext(TenantB()))
            await _store.SaveAsync(Item("shared", "from-b"));

        Assert.Equal([("shared", "tenant-b", "from-b")], LoadItems());
    }

    [Fact]
    public async Task IncomingAgnosticSave_AgainstConcreteTenant_Throws_AndLeavesRowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Item("shared", "owned-by-a"));

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "star-takeover", Tenant.AgnosticTenantId)));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    public void Dispose()
    {
        using (var connection = _provider.GetConnection())
            connection.Execute($"drop table {Q(_table)}");
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private void Seed(string id, string? tenantId, string name)
    {
        using var connection = _provider.GetConnection();
        connection.Execute(
            $"insert into {Q(_table)} ({Q("Id")}, {Q("TenantId")}, {Q("Name")}) values (@id, @tenantId, @name)",
            new { id, tenantId, name });
    }

    private IReadOnlyList<(string Id, string? TenantId, string Name)> LoadItems()
    {
        using var connection = _provider.GetConnection();
        return connection.Query<(string, string?, string)>($"select {Q("Id")}, {Q("TenantId")}, {Q("Name")} from {Q(_table)} order by {Q("Id")}").ToList();
    }

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static ItemRecord Item(string id, string name, string? tenantId = null) => new()
    {
        Id = id,
        Name = name,
        TenantId = tenantId
    };

    internal sealed class ItemRecord : Elsa.Persistence.Dapper.Records.Record
    {
        public string Name { get; set; } = null!;
    }
}
