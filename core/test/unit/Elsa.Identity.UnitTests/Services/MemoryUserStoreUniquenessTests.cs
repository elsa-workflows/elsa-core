using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory must enforce the same per-tenant user name uniqueness that EF Core
/// already enforces via <c>PerTenantIdentityUniqueness</c> on <c>(TenantId, Name)</c>.
/// </summary>
public class MemoryUserStoreUniquenessTests
{
    [Fact(DisplayName = "SaveAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task SaveAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateUser("user-1", "alice", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateUser("user-2", "alice", "tenant-a")));

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new UserFilter())).ToList();
        Assert.Equal("user-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "SaveAsync allows the same Id to update its own name")]
    public async Task SaveAsync_WhenSameIdUpdatesName_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateUser("user-1", "alice", "tenant-a"));

        await store.SaveAsync(CreateUser("user-1", "bob", "tenant-a"));

        var stored = await store.FindAsync(new UserFilter { Id = "user-1" });
        Assert.Equal("bob", stored!.Name);
    }

    [Fact(DisplayName = "SaveAsync allows the same name in different tenants")]
    public async Task SaveAsync_WhenNameRepeatsInAnotherTenant_Succeeds()
    {
        var backing = new MemoryStore<User>();
        var tenantA = new MemoryUserStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryUserStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(CreateUser("user-a", "alice", "tenant-a"));
        await tenantB.SaveAsync(CreateUser("user-b", "alice", "tenant-b"));

        Assert.Equal("alice", (await tenantA.FindAsync(new UserFilter { Id = "user-a" }))!.Name);
        Assert.Equal("alice", (await tenantB.FindAsync(new UserFilter { Id = "user-b" }))!.Name);
    }

    [Fact(DisplayName = "SaveAsync rejects renaming onto a name another Id already owns")]
    public async Task SaveAsync_WhenRenamingOntoAnotherIdsName_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateUser("user-1", "alice", "tenant-a"));
        await store.SaveAsync(CreateUser("user-2", "bob", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateUser("user-2", "alice", "tenant-a")));

        Assert.Contains("already exists", exception.Message);
        Assert.Equal("bob", (await store.FindAsync(new UserFilter { Id = "user-2" }))!.Name);
    }

    [Fact(DisplayName = "SaveAsync treats a stamped ambient tenant as the uniqueness tenant")]
    public async Task SaveAsync_WhenTenantIdUnset_UsesStampedAmbientTenantForUniqueness()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateUser("user-1", "alice", tenantId: null));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateUser("user-2", "alice", tenantId: null)));

        Assert.Contains("already exists", exception.Message);
        Assert.Equal("tenant-a", (await store.FindAsync(new UserFilter { Id = "user-1" }))!.TenantId);
    }

    [Fact(DisplayName = "SaveAsync allows the same name for * and a tenant-scoped user")]
    public async Task SaveAsync_WhenAgnosticAndTenantScopedShareName_Succeeds()
    {
        var store = CreateStore("tenant-a");

        await store.SaveAsync(CreateUser("user-star", "alice", Tenant.AgnosticTenantId));
        await store.SaveAsync(CreateUser("user-a", "alice", "tenant-a"));

        Assert.NotNull(await store.FindAsync(new UserFilter { Id = "user-star" }));
        Assert.NotNull(await store.FindAsync(new UserFilter { Id = "user-a" }));
    }

    private static MemoryUserStore CreateStore(string tenantId) =>
        new(new MemoryStore<User>(), new TestTenantAccessor(tenantId));

    private static User CreateUser(string id, string name, string? tenantId) =>
        new()
        {
            Id = id,
            Name = name,
            TenantId = tenantId
        };
}
