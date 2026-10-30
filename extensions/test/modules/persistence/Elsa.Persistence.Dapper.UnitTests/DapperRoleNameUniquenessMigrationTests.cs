using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.Dapper.Migrations.Identity;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Modules.Identity.Stores;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// The Identity V3_10 unique index on Roles (TenantId, Name), applied to a database shaped by the earlier Identity
/// migrations (elsa-core#8615, elsa-extensions#282).
/// </summary>
public sealed class DapperRoleNameUniquenessMigrationTests : IDisposable
{
    private const long IdentityV3_3 = 30004;
    private const long IdentityV3_10 = 30005;
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-dapper-role-names-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;

    public DapperRoleNameUniquenessMigrationTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
    }

    [Fact]
    public async Task ExistingDataWithASameNamedRoleInTwoTenantsMigratesAndKeepsLegacyIds()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("role-b-admin", "admin", "tenant-b", "perm-b");
        InsertRole("power-user", "power-user", "tenant-b", "perm-pu");

        MigrateUp();

        Assert.True(IndexExists());
        Assert.True(VersionApplied(IdentityV3_10));
        Assert.Equal(
            [
                ("admin", "admin", "tenant-a"),
                ("power-user", "power-user", "tenant-b"),
                ("role-b-admin", "admin", "tenant-b")
            ],
            LoadRoles());

        var tenantAccessor = new RoleTestTenantAccessor();
        var store = CreateStore(tenantAccessor);

        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            var legacy = await store.FindAsync(new RoleFilter { Id = "admin" });
            Assert.Equal("admin", legacy?.Name);
            Assert.Equal(["admin"], (await store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).Select(x => x.Id));
        }

        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            Assert.Equal("admin", (await store.FindAsync(new RoleFilter { Id = "role-b-admin" }))?.Name);
            // A legacy ID that differs from the role name now resolves by ID.
            Assert.Equal(["power-user"], (await store.FindManyAsync(new RoleFilter { Ids = ["power-user"] })).Select(x => x.Id));
        }
    }

    [Fact]
    public void SameTenantExactDuplicatesFailTheMigrationAndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("admin-dup", "admin", "tenant-a", "perm-dup");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        var text = exception.ToString();
        Assert.Contains(V3_10.TenantIdNameUniqueIndex, text, StringComparison.Ordinal);
        Assert.Contains("tenant 'tenant-a'", text, StringComparison.Ordinal);
        Assert.Contains("Id=admin, Id=admin-dup", text, StringComparison.Ordinal);
        Assert.Contains("No rows were changed", text, StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.False(VersionApplied(IdentityV3_10));
        Assert.Equal([("admin", "admin", "tenant-a"), ("admin-dup", "admin", "tenant-a")], LoadRoles());
    }

    [Fact]
    public void SameTenantCaseVariantsFailTheMigrationAndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("admin-cased", "Admin", "tenant-a", "perm-cased");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        var text = exception.ToString();
        Assert.Contains("'admin' / 'Admin'", text, StringComparison.Ordinal);
        Assert.Contains("Id=admin, Id=admin-cased", text, StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.Equal([("admin", "admin", "tenant-a"), ("admin-cased", "Admin", "tenant-a")], LoadRoles());
    }

    [Fact]
    public void NullAndEmptyDefaultTenantRowsWithTheSameNameFailTheMigrationAndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin-null", "admin", null, "perm-null");
        InsertRole("admin-empty", "admin", "", "perm-empty");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        var text = exception.ToString();
        Assert.Contains("tenant (default)", text, StringComparison.Ordinal);
        Assert.Contains("Id=admin-empty, Id=admin-null", text, StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.False(VersionApplied(IdentityV3_10));
        Assert.Equal([("admin-empty", "admin", ""), ("admin-null", "admin", null)], LoadRoles());
    }

    [Fact]
    public void ResolvingTheDuplicatesLetsTheMigrationRunAgain()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("admin-dup", "admin", "tenant-a", "perm-dup");
        Assert.NotNull(Record.Exception(() => MigrateUp()));

        using (var connection = new SqliteConnection(_connectionString))
            connection.Execute("update Roles set Name = 'admin-2' where Id = 'admin-dup'");

        MigrateUp();

        Assert.True(IndexExists());
        Assert.True(VersionApplied(IdentityV3_10));
    }

    [Fact]
    public async Task AfterTheMigrationASameTenantDuplicateIsRejected()
    {
        MigrateUp(IdentityV3_3);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("role-b-admin", "admin", "tenant-b", "perm-b");
        MigrateUp();

        var tenantAccessor = new RoleTestTenantAccessor();
        var store = CreateStore(tenantAccessor);

        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            Assert.NotNull(await Record.ExceptionAsync(() => store.SaveAsync(new Role { Id = "role-a-dup", Name = "admin", Permissions = ["perm-dup"] })));

        await using (var connection = new SqliteConnection(_connectionString))
        {
            Assert.NotNull(await Record.ExceptionAsync(() => connection.ExecuteAsync(
                "insert into Roles (Id, Name, Permissions, TenantId) values ('raw-dup', 'admin', 'x', 'tenant-a')")));
        }

        Assert.Equal([("admin", "admin", "tenant-a"), ("role-b-admin", "admin", "tenant-b")], LoadRoles());
    }

    [Fact]
    public void RollingBackV3_10DropsTheIndex()
    {
        MigrateUp();
        Assert.True(IndexExists());

        MigrateDown(IdentityV3_3);

        Assert.False(IndexExists());
        Assert.False(VersionApplied(IdentityV3_10));
    }

    public void Dispose() => File.Delete(_databasePath);

    private DapperRoleStore CreateStore(ITenantAccessor tenantAccessor) =>
        new(new Store<RoleRecord>(new SqliteDbConnectionProvider(_connectionString), tenantAccessor, "Roles"));

    private void InsertRole(string id, string name, string? tenantId, string permissions)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(
            "insert into Roles (Id, Name, Permissions, TenantId) values (@id, @name, @permissions, @tenantId)",
            new { id, name, permissions, tenantId });
    }

    private IReadOnlyList<(string Id, string Name, string? TenantId)> LoadRoles()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<(string, string, string?)>("select Id, Name, TenantId from Roles order by Id").ToList();
    }

    private bool IndexExists()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            "select count(*) from sqlite_master where type = 'index' and name = @name",
            new { name = V3_10.TenantIdNameUniqueIndex }) == 1;
    }

    private bool VersionApplied(long version)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>("select count(*) from VersionInfo where Version = @version", new { version }) == 1;
    }

    private void MigrateUp(long? targetVersion = null)
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (targetVersion is { } version)
        {
            runner.MigrateUp(version);
        }
        else
        {
            runner.MigrateUp();
        }
    }

    private void MigrateDown(long targetVersion)
    {
        using var services = CreateMigrator();
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateDown(targetVersion);
    }

    private ServiceProvider CreateMigrator() =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);
}
