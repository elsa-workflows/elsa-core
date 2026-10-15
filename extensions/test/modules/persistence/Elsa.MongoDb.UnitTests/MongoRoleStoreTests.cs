using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Identity;
using MongoDB.Driver;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Tenant-safe MongoDB role lookups and saves (elsa-core#8615). <see cref="MongoRoleStore"/> filters through
/// <see cref="RoleFilter.Apply"/> and saves through <see cref="MongoDbStore{TDocument}"/>'s tenant-owned upsert.
/// </summary>
public sealed class MongoRoleStoreTests : IClassFixture<RoleMongoFixture>, IDisposable
{
    private readonly MongoClient _client;
    private readonly MongoRoleStore _store;
    private readonly RoleTestTenantAccessor _tenantAccessor = new();

    public MongoRoleStoreTests(RoleMongoFixture fixture)
    {
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase($"elsa-role-store-{Guid.NewGuid():N}");
        _store = new MongoRoleStore(new MongoDbStore<Role>(database.GetCollection<Role>("roles"), _tenantAccessor));
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task LegacyNameDerivedIdStillResolvesByIdAndIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("admin", "admin", "perm-a"));

        Assert.Equal("admin", (await _store.FindAsync(new RoleFilter { Id = "admin" }))?.Name);
        Assert.Equal(["admin"], (await _store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).Select(x => x.Id));
    }

    [Fact]
    public async Task GeneratedIdResolvesThroughIdsAndNameFilter()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("role-generated-1", "admin", "perm-a"));

        Assert.Equal(["role-generated-1"], (await _store.FindManyAsync(new RoleFilter { Ids = ["role-generated-1"] })).Select(x => x.Id));
        Assert.Equal(["role-generated-1"], (await _store.FindManyAsync(new RoleFilter { Name = "admin" })).Select(x => x.Id));
        Assert.Null(await _store.FindAsync(new RoleFilter { Id = "admin" }));
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
            tenantARole = await _store.FindAsync(new RoleFilter { Name = "operators" });

        Role? tenantBRole;
        using (_tenantAccessor.PushContext(TenantB()))
            tenantBRole = await _store.FindAsync(new RoleFilter { Name = "operators" });

        Assert.Equal(("role-a", "tenant-a"), (tenantARole?.Id, tenantARole?.TenantId));
        Assert.Equal(("role-b", "tenant-b"), (tenantBRole?.Id, tenantBRole?.TenantId));
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

        Assert.NotNull(exception);
        Assert.True(MongoErrors.IsDuplicateKey(exception), exception.ToString());

        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        var stored = await _store.FindAsync(new RoleFilter { Id = "admin" });
        Assert.Equal(("tenant-a", "perm-a-updated"), (stored?.TenantId, stored?.Permissions.Single()));
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

internal static class MongoErrors
{
    public static bool IsDuplicateKey(Exception exception) =>
        exception switch
        {
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey || write.WriteError.Code == 11000,
            MongoCommandException command => command.Code == 11000,
            MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey || error.Code == 11000),
            _ => exception.InnerException is { } inner && IsDuplicateKey(inner)
        };
}
