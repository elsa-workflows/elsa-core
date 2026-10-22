using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-scoped Dapper <see cref="Store{T}"/> upserts and reads (#9010). Defaults to SQLite; set
/// <c>ELSA_DAPPER_CAS_PROVIDER</c> / <c>ELSA_DAPPER_CAS_CONNECTION</c> to run on PostgreSQL or SQL Server.
/// </summary>
public sealed class DapperStoreTenantUpsertTests : IDisposable
{
    private readonly string _backend = Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_PROVIDER") ?? "sqlite";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"elsa-dapper-store-upsert-{Guid.NewGuid():N}.db");
    private readonly string _table = $"Items_{Guid.NewGuid():N}";
    private readonly IDbConnectionProvider _provider;
    private readonly TenantConflictTestStore<ItemRecord> _store;
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
        {
            schema = $"create table {Q(_table)} ({Q("Id")} varchar(255) not null primary key, {Q("TenantId")} nvarchar(255) null, {Q("Name")} nvarchar(max) not null)";
        }

        connection.Execute(schema);

        _store = new TenantConflictTestStore<ItemRecord>(_provider, _tenantAccessor, _table);
    }

    [Fact]
    public async Task CrossTenantIdCollision_Throws_AndLeavesTenantARowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "owned-by-a"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "taken-by-b")));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("3.10.1 upgrade step", invalidOperation.Message, StringComparison.Ordinal);
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
        {
            await _store.SaveAsync(Item("clash", "owned-by-a"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveManyAsync([Item("fresh", "from-b"), Item("clash", "taken-by-b")]));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Contains(("clash", "tenant-a", "owned-by-a"), LoadItems());
    }

    [Fact]
    public async Task DefaultTenant_UpdatesLegacyNullTenantRow_AndDoesNotThrow()
    {
        Seed("legacy", tenantId: null, name: "original");

        using (_tenantAccessor.PushContext(Tenant.Default))
        {
            await _store.SaveAsync(Item("legacy", "updated"));
        }

        var row = Assert.Single(LoadItems());
        Assert.Equal("legacy", row.Id);
        Assert.Equal("updated", row.Name);
        Assert.True(string.IsNullOrEmpty(row.TenantId));
    }

    [Fact]
    public async Task ExistingAgnosticRow_SavedByConcreteTenant_KeepsStarTenant()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");

        using (_tenantAccessor.PushContext(TenantB()))
        {
            await _store.SaveAsync(Item("shared", "from-b", Tenant.AgnosticTenantId));
        }

        Assert.Equal([("shared", Tenant.AgnosticTenantId, "from-b")], LoadItems());
    }

    [Fact]
    public async Task ExistingAgnosticRow_SavedByConcreteTenantWithAmbientStamp_Throws_AndKeepsStar()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "from-b")));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", Tenant.AgnosticTenantId, "shared-star")], LoadItems());
    }

    [Fact]
    public async Task IncomingAgnosticSave_AgainstConcreteTenant_Throws_AndLeavesRowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "owned-by-a"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "star-takeover", Tenant.AgnosticTenantId)));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    [Fact]
    public async Task ExplicitConcreteTenantDifferentFromAmbient_Throws()
    {
        Exception? exception;
        using (_tenantAccessor.PushContext(TenantA()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("forged", "nope", "tenant-b")));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("does not match the current tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Empty(LoadItems());
    }

    [Fact]
    public async Task AmbientAgnostic_SavesAndUpdatesSharedRow()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _store.SaveAsync(Item("shared", "original"));
            await _store.SaveAsync(Item("shared", "updated"));
        }

        Assert.Equal([("shared", Tenant.AgnosticTenantId, "updated")], LoadItems());
    }

    [Fact]
    public async Task NullTenantContext_KeepsExplicitTenantAndCanInsert()
    {
        using (_tenantAccessor.PushContext(null))
        {
            await _store.SaveAsync(Item("owned", "from-system", "tenant-a"));
        }

        Assert.Equal([("owned", "tenant-a", "from-system")], LoadItems());
    }

    [Fact]
    public async Task ConcreteTenantRead_ReturnsAgnosticRows()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");
        Seed("owned-a", "tenant-a", "only-a");
        Seed("owned-b", "tenant-b", "only-b");

        IReadOnlyList<ItemRecord> tenantA;
        using (_tenantAccessor.PushContext(TenantA()))
        {
            tenantA = (await _store.FindManyAsync(_ => { }, CancellationToken.None)).ToList();
        }

        IReadOnlyList<ItemRecord> tenantB;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            tenantB = (await _store.FindManyAsync(_ => { }, CancellationToken.None)).ToList();
        }

        Assert.Equal(["owned-a", "shared"], tenantA.Select(x => x.Id).OrderBy(x => x));
        Assert.Equal(["owned-b", "shared"], tenantB.Select(x => x.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task DefaultTenantRead_ReturnsAgnosticAndNullRows()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");
        Seed("legacy", tenantId: null, name: "null-owned");
        Seed("owned-a", "tenant-a", "only-a");

        IReadOnlyList<ItemRecord> found;
        using (_tenantAccessor.PushContext(Tenant.Default))
        {
            found = (await _store.FindManyAsync(_ => { }, CancellationToken.None)).ToList();
        }

        Assert.Equal(["legacy", "shared"], found.Select(x => x.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task UpdateAsync_CrossTenant_Throws_AndLeavesRowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "owned-by-a"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.UpdateAsync(Item("shared", "taken-by-b")));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    [Fact]
    public async Task UpdateAsync_SameTenant_UpdatesTheExistingRow()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "original"));
            await _store.UpdateAsync(Item("shared", "updated"));
        }

        Assert.Equal([("shared", "tenant-a", "updated")], LoadItems());
    }

    [Fact]
    public async Task UpdateAsync_ExistingAgnosticRow_KeepsStarTenant()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");

        using (_tenantAccessor.PushContext(TenantB()))
        {
            await _store.UpdateAsync(Item("shared", "from-b", Tenant.AgnosticTenantId));
        }

        Assert.Equal([("shared", Tenant.AgnosticTenantId, "from-b")], LoadItems());
    }

    [Fact]
    public async Task ZeroRowUpsert_SameTenantLookup_RetriesAndWrites()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "payload-1"));
            _store.ForcedAffectedRows = 0;
            await _store.SaveAsync(Item("shared", "payload-2"));
        }

        Assert.Equal([("shared", "tenant-a", "payload-2")], LoadItems());
    }

    [Fact]
    public async Task ZeroRowUpsert_StaleAgnosticLookupAfterConcreteClaim_ThrowsAndLeavesClaimedRow()
    {
        Seed("shared", Tenant.AgnosticTenantId, "shared-star");

        _store.ForcedAffectedRows = 0;
        _store.AfterExistingRowLookup = (_, _) =>
        {
            using var connection = _provider.GetConnection();
            connection.Execute(
                $"update {Q(_table)} set {Q("TenantId")} = @tenantId, {Q("Name")} = @name where {Q("Id")} = @id",
                new { id = "shared", tenantId = "tenant-a", name = "from-a" });
        };

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "from-b", Tenant.AgnosticTenantId)));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("Failed to save", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "from-a")], LoadItems());
    }

    [Fact]
    public async Task ZeroRowUpsert_MinusOneAffectedRows_ExistingOwnRow_IsAccepted()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "payload-1"));
            _store.ForcedAffectedRows = -1;
            await _store.SaveAsync(Item("shared", "payload-1"));
        }

        Assert.Equal([("shared", "tenant-a", "payload-1")], LoadItems());
    }

    [Fact]
    public async Task ZeroRowUpsert_MinusOneAffectedRows_ExistingForeignTenant_Throws()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "owned-by-a"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            _store.ForcedAffectedRows = -1;
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "from-b")));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("3.10.1 upgrade step", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    [Fact]
    public async Task UpdateAsync_MissingRow_IsNoOp()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            var exception = await Record.ExceptionAsync(() => _store.UpdateAsync(Item("missing", "gone")));
            Assert.Null(exception);
        }

        Assert.Empty(LoadItems());
    }

    [Fact]
    public async Task UpdateAsync_MissingRow_PropsOverload_IsNoOp()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            var exception = await Record.ExceptionAsync(() => _store.UpdateAsync(Item("missing", "gone"), [x => x.Name]));
            Assert.Null(exception);
        }

        Assert.Empty(LoadItems());
    }

    [Fact]
    public async Task UpdateAsync_ZeroRow_SameTenantLookup_DoesNotRetry()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "payload-1"));
            _store.ForcedAffectedRows = 0;
            await _store.UpdateAsync(Item("shared", "payload-2"));
        }

        Assert.Equal([("shared", "tenant-a", "payload-1")], LoadItems());
    }

    public void Dispose()
    {
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"drop table {Q(_table)}");
        }

        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
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
    private static Tenant AgnosticTenant() => new() { Id = Tenant.AgnosticTenantId };

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
