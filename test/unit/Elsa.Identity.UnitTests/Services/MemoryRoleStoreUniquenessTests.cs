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
    [Test]
    [DisplayName("SaveAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task SaveAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateRole("role-2", "Operators", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        var stored = (await store.FindManyAsync(new RoleFilter())).ToList();
        await Assert.That((await Assert.That(stored).HasSingleItem()).Id).IsEqualTo("role-1");
    }

    [Test]
    [DisplayName("AddAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task AddAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.AddAsync(CreateRole("role-1", "Operators", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.AddAsync(CreateRole("role-2", "Operators", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        var stored = (await store.FindManyAsync(new RoleFilter())).ToList();
        await Assert.That((await Assert.That(stored).HasSingleItem()).Id).IsEqualTo("role-1");
    }

    [Test]
    [DisplayName("AddAsync rejects a role ID already used by another tenant")]
    public async Task AddAsync_WhenIdExistsInAnotherTenant_Throws()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.AddAsync(CreateRole("shared-role", "Tenant A role", "tenant-a"));

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            tenantB.AddAsync(CreateRole("shared-role", "Tenant B role", "tenant-b")));

        await Assert.That((await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.TenantId).IsEqualTo("tenant-a");
        await Assert.That(await tenantB.FindAsync(new RoleFilter { Id = "shared-role" })).IsNull();
    }

    [Test]
    [DisplayName("AddAsync admits only one concurrent writer for a globally unique role ID")]
    public async Task AddAsync_WhenConcurrentWritersUseTheSameId_AllButOneFailAtomically()
    {
        static async Task<Exception?> TryAddAsync(MemoryRoleStore store, Role role)
        {
            try
            {
                await store.AddAsync(role);
                return null;
            }
            catch (ArgumentException exception)
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

        await Assert.That(exceptions).HasSingleItem(exception => exception is null);
        foreach (var exception in exceptions.Where(exception => exception is not null))
        {
            await Assert.That(exception).IsTypeOf<ArgumentException>();
        }

        await Assert.That(backing.List()).HasSingleItem();
    }

    [Test]
    [DisplayName("SaveAsync rejects a role ID already used by another tenant")]
    public async Task SaveAsync_WhenIdExistsInAnotherTenant_Throws()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        var tenantARole = CreateRole("shared-role", "Tenant A role", "tenant-a");
        tenantARole.Permissions = ["tenant-a:permission"];
        await tenantA.SaveAsync(tenantARole);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            tenantB.SaveAsync(CreateRole("shared-role", "Tenant B role", "tenant-b")));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That((await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.TenantId).IsEqualTo("tenant-a");
        await Assert.That(await tenantB.FindAsync(new RoleFilter { Id = "shared-role" })).IsNull();

        var roleTaggedAsOwner = CreateRole("shared-role", "Tenant A replacement", "tenant-a");
        roleTaggedAsOwner.Permissions = ["tenant-b:permission"];
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(roleTaggedAsOwner));

        var roleRehomedFromTenantA = CreateRole("shared-role", "Rehomed role", "tenant-b");
        roleRehomedFromTenantA.Permissions = ["tenant-b:permission"];
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantA.SaveAsync(roleRehomedFromTenantA));

        var roleMadeAgnostic = CreateRole("shared-role", "Agnostic role", Tenant.AgnosticTenantId);
        roleMadeAgnostic.Permissions = ["tenant-b:permission"];
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantA.SaveAsync(roleMadeAgnostic));

        var unchanged = await tenantA.FindAsync(new RoleFilter { Id = "shared-role" });
        await Assert.That(unchanged!.Name).IsEqualTo("Tenant A role");
        await Assert.That(unchanged.TenantId).IsEqualTo("tenant-a");
        await Assert.That(unchanged.Permissions).IsEquivalentTo(["tenant-a:permission"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("SaveAsync allows a named tenant to update a visible agnostic role")]
    public async Task SaveAsync_WhenAgnosticRoleIsVisibleToNamedTenant_UpdatesExistingRole()
    {
        var backing = new MemoryStore<Role>();
        var agnostic = new MemoryRoleStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));

        await agnostic.SaveAsync(CreateRole("shared-role", "Shared role", Tenant.AgnosticTenantId));

        await tenantA.SaveAsync(CreateRole("shared-role", "Updated shared role", Tenant.AgnosticTenantId));

        var stored = await agnostic.FindAsync(new RoleFilter { Id = "shared-role" });
        await Assert.That(stored!.Name).IsEqualTo("Updated shared role");
        await Assert.That(stored.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That((await tenantA.FindAsync(new RoleFilter { Id = "shared-role" }))!.Name).IsEqualTo("Updated shared role");
    }

    [Test]
    [DisplayName("SaveAsync allows the same Id to update its own name")]
    public async Task SaveAsync_WhenSameIdUpdatesName_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));

        await store.SaveAsync(CreateRole("role-1", "Operators A", "tenant-a"));

        var stored = await store.FindAsync(new RoleFilter { Id = "role-1" });
        await Assert.That(stored!.Name).IsEqualTo("Operators A");
    }

    [Test]
    [DisplayName("SaveAsync allows the same name in different tenants")]
    public async Task SaveAsync_WhenNameRepeatsInAnotherTenant_Succeeds()
    {
        var backing = new MemoryStore<Role>();
        var tenantA = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryRoleStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(CreateRole("role-a", "Operators", "tenant-a"));
        await tenantB.SaveAsync(CreateRole("role-b", "Operators", "tenant-b"));

        await Assert.That((await tenantA.FindAsync(new RoleFilter { Id = "role-a" }))!.Name).IsEqualTo("Operators");
        await Assert.That((await tenantB.FindAsync(new RoleFilter { Id = "role-b" }))!.Name).IsEqualTo("Operators");
    }

    [Test]
    [DisplayName("SaveAsync leaves the stored name unchanged when a Find result is renamed onto a collision")]
    public async Task SaveAsync_WhenFoundRoleRenamedOntoCollision_LeavesStoredNameUnchanged()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));
        await store.SaveAsync(CreateRole("role-2", "Reviewers", "tenant-a"));

        var found = await store.FindAsync(new RoleFilter { Id = "role-2" });
        found!.Name = "Operators";

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveAsync(found));

        await Assert.That(exception!.Message).Contains("already exists");
        var stored = await store.FindAsync(new RoleFilter { Id = "role-2" });
        await Assert.That(stored!.Name).IsEqualTo("Reviewers");
    }

    [Test]
    [DisplayName("SaveAsync rejects renaming onto a name another Id already owns")]
    public async Task SaveAsync_WhenRenamingOntoAnotherIdsName_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateRole("role-1", "Operators", "tenant-a"));
        await store.SaveAsync(CreateRole("role-2", "Reviewers", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateRole("role-2", "Operators", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That((await store.FindAsync(new RoleFilter { Id = "role-2" }))!.Name).IsEqualTo("Reviewers");
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
