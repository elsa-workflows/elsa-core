using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory must enforce the same global role ID and per-tenant role name uniqueness
/// that EF Core already enforces on the Roles table.
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

    [Fact(DisplayName = "AddAsync rejects a role ID already used by another tenant")]
    public async Task AddAsync_WhenIdExistsInAnotherTenant_Throws()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.AddAsync(CreateRole("shared-role", "Tenant A role", "tenant-a"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tenantB.AddAsync(CreateRole("shared-role", "Tenant B role", "tenant-b")));

        Assert.Equal("tenant-a", (await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.TenantId);
        Assert.Null(await tenantB.FindAsync(new RoleFilter { Id = "shared-role" }));
    }

    [Fact(DisplayName = "AddAsync admits only one concurrent writer for a globally unique role ID")]
    public async Task AddAsync_WhenConcurrentWritersUseTheSameId_AllButOneFailAtomically()
    {
        static async Task<Exception?> TryAddAsync(MemoryRoleStore store, Role role)
        {
            try
            {
                await store.AddAsync(role);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var backing = new MemoryStore<Role>();
        var stores = new[]
        {
            new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a")),
            new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"))
        };

        var attempts = Enumerable.Range(0, 32)
            .Select(index => Task.Run(() => TryAddAsync(
                stores[index % stores.Length],
                CreateRole("concurrent-role", $"Role {index}", $"tenant-{(char)('a' + index % stores.Length)}"))))
            .ToArray();
        var exceptions = await Task.WhenAll(attempts);

        Assert.Single(exceptions, exception => exception is null);
        Assert.All(exceptions.Where(exception => exception is not null), exception => Assert.IsType<ArgumentException>(exception));
        Assert.Single(backing.List());
    }

    [Fact(DisplayName = "SaveAsync rejects a role ID already used by another tenant")]
    public async Task SaveAsync_WhenIdExistsInAnotherTenant_Throws()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(CreateRole("shared-role", "Tenant A role", "tenant-a"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenantB.SaveAsync(CreateRole("shared-role", "Tenant B role", "tenant-b")));

        Assert.Contains("already exists", exception.Message);
        Assert.Equal("tenant-a", (await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.TenantId);
        Assert.Null(await tenantB.FindAsync(new RoleFilter { Id = "shared-role" }));

        var roleTaggedAsOwner = CreateRole("shared-role", "Tenant A replacement", "tenant-a");
        await Assert.ThrowsAsync<InvalidOperationException>(() => tenantB.SaveAsync(roleTaggedAsOwner));

        var unchanged = await tenantA.FindAsync(new RoleFilter { Id = "shared-role" });
        Assert.Equal("Tenant A role", unchanged!.Name);
        Assert.Equal("tenant-a", unchanged.TenantId);
    }

    [Fact(DisplayName = "SaveAsync does not let a named tenant replace an agnostic role")]
    public async Task SaveAsync_WhenAgnosticRoleIsVisibleToNamedTenant_ThrowsWithoutOverwriting()
    {
        var backing = new MemoryStore<Role>();
        var agnostic = new MemoryRoleStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));

        await agnostic.SaveAsync(CreateRole("shared-role", "Shared role", Tenant.AgnosticTenantId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenantA.SaveAsync(CreateRole("shared-role", "Updated shared role", Tenant.AgnosticTenantId)));

        var stored = await agnostic.FindAsync(new RoleFilter { Id = "shared-role" });
        Assert.Equal("Shared role", stored!.Name);
        Assert.Equal(Tenant.AgnosticTenantId, stored.TenantId);
        Assert.Equal("Shared role", (await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.Name);

        await agnostic.SaveAsync(CreateRole("shared-role", "Updated shared role", Tenant.AgnosticTenantId));
        Assert.Equal("Updated shared role", (await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.Name);
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

        await tenantA.SaveAsync(CreateRole("role-a", "Operators", "tenant-a"));
        await tenantB.SaveAsync(CreateRole("role-b", "Operators", "tenant-b"));

        Assert.Equal("Operators", (await tenantA.FindAsync(new RoleFilter { Id = "role-a" }))!.Name);
        Assert.Equal("Operators", (await tenantB.FindAsync(new RoleFilter { Id = "role-b" }))!.Name);
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
