using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Modules.Identity.Stores;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-safe Dapper role lookups and saves (elsa-core#8615). Role IDs are no longer derived from the name, so
/// <see cref="RoleFilter.Ids"/> must match the Id column, and saving must never move another tenant's row.
/// </summary>
public sealed class DapperRoleStoreTests : IDisposable
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-dapper-roles-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly DapperRoleStore _store;
    private readonly RoleTestTenantAccessor _tenantAccessor = new();

    public DapperRoleStoreTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute("""
                           create table Roles (
                               Id text not null primary key,
                               Name text not null,
                               Permissions text not null,
                               TenantId text null
                           );
                           """);

        _store = new(new Store<RoleRecord>(new SqliteDbConnectionProvider(_connectionString), _tenantAccessor, "Roles"));
    }

    [Fact]
    public async Task LegacyNameDerivedIdStillResolvesByIdAndIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("admin", "admin", "perm-a"));

        var byId = await _store.FindAsync(new RoleFilter { Id = "admin" });
        var byIds = (await _store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).ToList();

        Assert.NotNull(byId);
        Assert.Equal("admin", byId.Name);
        Assert.Equal(["admin"], byIds.Select(x => x.Id));
    }

    [Fact]
    public async Task GeneratedIdResolvesThroughIdsAndIdsDoNotMatchTheName()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("role-generated-1", "admin", "perm-a"));

        var byIds = (await _store.FindManyAsync(new RoleFilter { Ids = ["role-generated-1"] })).ToList();
        var byNameAsId = (await _store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).ToList();
        var byId = await _store.FindAsync(new RoleFilter { Id = "role-generated-1" });

        Assert.Equal(["role-generated-1"], byIds.Select(x => x.Id));
        Assert.Empty(byNameAsId);
        Assert.NotNull(byId);
        Assert.Equal("admin", byId.Name);
    }

    [Fact]
    public async Task NameFilterMatchesOnlyThatNameInTheCurrentTenant()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Role("role-a-admin", "admin", "perm-a"));
            await _store.SaveAsync(Role("role-a-operators", "operators", "perm-a"));
        }

        using (_tenantAccessor.PushContext(TenantB()))
            await _store.SaveAsync(Role("role-b-admin", "admin", "perm-b"));

        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        var byName = (await _store.FindManyAsync(new RoleFilter { Name = "admin" })).ToList();
        await _store.DeleteAsync(new RoleFilter { Name = "operators" });
        var remaining = (await _store.FindManyAsync(new RoleFilter())).ToList();

        Assert.Equal(["role-a-admin"], byName.Select(x => x.Id));
        Assert.Equal(["role-a-admin"], remaining.Select(x => x.Id));

        using (_tenantAccessor.PushContext(TenantB()))
            Assert.Equal(["role-b-admin"], (await _store.FindManyAsync(new RoleFilter())).Select(x => x.Id));
    }

    [Fact]
    public async Task TwoTenantsCanHoldASameNamedRole()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "operators", "perm-a"));

        using (_tenantAccessor.PushContext(TenantB()))
            await _store.SaveAsync(Role("role-b", "operators", "perm-b"));

        Role? tenantARole;
        using (_tenantAccessor.PushContext(TenantA()))
            tenantARole = await _store.FindAsync(new RoleFilter { Id = "role-a" });

        Role? tenantBRole;
        using (_tenantAccessor.PushContext(TenantB()))
            tenantBRole = await _store.FindAsync(new RoleFilter { Id = "role-b" });

        Assert.Equal(("operators", "tenant-a"), (tenantARole?.Name, tenantARole?.TenantId));
        Assert.Equal(("operators", "tenant-b"), (tenantBRole?.Name, tenantBRole?.TenantId));
    }

    [Fact]
    public async Task ARoleInTenantAIsInvisibleToTenantB()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "operators", "perm-a"));

        using var tenantScope = _tenantAccessor.PushContext(TenantB());

        Assert.Null(await _store.FindAsync(new RoleFilter { Id = "role-a" }));
        Assert.Empty(await _store.FindManyAsync(new RoleFilter { Ids = ["role-a"] }));
        Assert.Empty(await _store.FindManyAsync(new RoleFilter()));
    }

    [Fact]
    public async Task SavingCannotTakeOverAnotherTenantsRow()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Role("admin", "admin", "perm-a"));
            await _store.SaveAsync(Role("admin", "admin", "perm-a-updated"));
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Role("admin", "admin", "perm-b")));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("admin", "admin", "perm-a-updated", "tenant-a")], LoadRoles());
    }

    [Fact]
    public async Task SavingCannotTakeOverATenantAgnosticRow()
    {
        await using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.ExecuteAsync(
                "insert into Roles (Id, Name, Permissions, TenantId) values ('admin', 'admin', 'perm-star', @tenantId)",
                new { tenantId = Tenant.AgnosticTenantId });
        }

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Role("admin", "admin", "perm-b")));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("tenant-agnostic ('*')", invalidOperation.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("admin", "admin", "perm-star", Tenant.AgnosticTenantId)], LoadRoles());
    }

    [Fact]
    public async Task UpdatingAnOwnedRoleChangesOnlyThatTenantsRow()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "operators", "perm-a"));

        using (_tenantAccessor.PushContext(TenantB()))
            await _store.SaveAsync(Role("role-b", "operators", "perm-b"));

        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "Operators", "perm-a-updated"));

        Assert.Equal(
            [
                ("role-a", "Operators", "perm-a-updated", "tenant-a"),
                ("role-b", "operators", "perm-b", "tenant-b")
            ],
            LoadRoles());
    }

    public void Dispose() => File.Delete(_databasePath);

    private IReadOnlyList<(string Id, string Name, string Permissions, string? TenantId)> LoadRoles()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<(string, string, string, string?)>("select Id, Name, Permissions, TenantId from Roles order by Id").ToList();
    }

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static Role Role(string id, string name, string permission) => new()
    {
        Id = id,
        Name = name,
        Permissions = [permission]
    };
}
