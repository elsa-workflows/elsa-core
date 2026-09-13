using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory must enforce the same per-tenant role name uniqueness that EF Core
/// already enforces via <c>PerTenantIdentityUniqueness</c> on <c>(TenantId, Name)</c>.
/// </summary>
public class MemoryRoleStoreUniquenessTests
{
    [Fact(DisplayName = "SaveAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task SaveAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateRole("role-2", "Operators", "tenant-a")));

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new RoleFilter())).ToList();
        Assert.Equal("role-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "AddAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task AddAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.AddAsync(CreateRole("role-1", "Operators", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.AddAsync(CreateRole("role-2", "Operators", "tenant-a")));

        Assert.Contains("already exists", exception.Message);
        var stored = (await store.FindManyAsync(new RoleFilter())).ToList();
        Assert.Equal("role-1", Assert.Single(stored).Id);
    }

    [Fact(DisplayName = "SaveAsync allows the same Id to update its own name")]
    public async Task SaveAsync_WhenSameIdUpdatesName_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));

        await store.SaveAsync(CreateRole("role-1", "Operators A", "tenant-a"));

        var stored = await store.FindAsync(new RoleFilter { Id = "role-1" });
        Assert.Equal("Operators A", stored!.Name);
    }

    [Fact(DisplayName = "SaveAsync allows the same name in different tenants")]
    public async Task SaveAsync_WhenNameRepeatsInAnotherTenant_Succeeds()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(CreateRole("operators", "Operators", "tenant-a"));
        await tenantB.SaveAsync(CreateRole("operators", "Operators", "tenant-b"));

        Assert.Equal("Operators", (await tenantA.FindAsync(new RoleFilter { Id = "operators" }))!.Name);
        Assert.Equal("Operators", (await tenantB.FindAsync(new RoleFilter { Id = "operators" }))!.Name);
    }

    [Fact(DisplayName = "SaveAsync leaves the stored name unchanged when a Find result is renamed onto a collision")]
    public async Task SaveAsync_WhenFoundRoleRenamedOntoCollision_LeavesStoredNameUnchanged()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));
        await store.SaveAsync(CreateRole("role-2", "Reviewers", "tenant-a"));

        var found = await store.FindAsync(new RoleFilter { Id = "role-2" });
        found!.Name = "Operators";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(found));

        Assert.Contains("already exists", exception.Message);
        var stored = await store.FindAsync(new RoleFilter { Id = "role-2" });
        Assert.Equal("Reviewers", stored!.Name);
    }

    [Fact(DisplayName = "SaveAsync rejects renaming onto a name another Id already owns")]
    public async Task SaveAsync_WhenRenamingOntoAnotherIdsName_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));
        await store.SaveAsync(CreateRole("role-2", "Reviewers", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateRole("role-2", "Operators", "tenant-a")));

        Assert.Contains("already exists", exception.Message);
        Assert.Equal("Reviewers", (await store.FindAsync(new RoleFilter { Id = "role-2" }))!.Name);
    }

    private static MemoryRoleStore CreateStore(string tenantId) =>
        new(new MemoryStore<Role>(), new TestTenantAccessor(tenantId));

    private static Role CreateRole(string id, string name, string? tenantId) =>
        new()
        {
            Id = id,
            Name = name,
            TenantId = tenantId,
            Permissions = []
        };
}
