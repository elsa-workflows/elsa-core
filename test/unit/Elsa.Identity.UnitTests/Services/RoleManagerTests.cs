using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class RoleManagerTests
{
    private readonly TestTenantAccessor _tenantAccessor;
    private readonly MemoryRoleStore _roleStore;
    private readonly RoleManager _manager;

    public RoleManagerTests()
    {
        _tenantAccessor = new TestTenantAccessor("tenant-a");
        _roleStore = new MemoryRoleStore(new MemoryStore<Role>(), _tenantAccessor);
        _manager = new RoleManager(_roleStore, new StoreBasedRoleProvider(_roleStore), _tenantAccessor);
    }

    [Test]
    public async Task CreateListUpdateAndDeleteAreIsolatedForRolesWithTheSameNameAcrossTenants()
    {
        var roleA = await _manager.CreateRoleAsync("Operators", ["tenant-a:permission"]);

        await Assert.That(roleA.Role.TenantId).IsEqualTo("tenant-a");
        await Assert.That(await _roleStore.FindManyAsync(new() { TenantId = "tenant-a" })).HasSingleItem();

        using (_tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            var roleB = await _manager.CreateRoleAsync("Operators", ["tenant-b:permission"]);

            await Assert.That(roleB.Role.Id).IsEqualTo(roleA.Role.Id);
            await Assert.That(roleB.Role.TenantId).IsEqualTo("tenant-b");
            await Assert.That(roleB.Role.Permissions).IsEquivalentTo(["tenant-b:permission"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

            var tenantBRoles = await _roleStore.FindManyAsync(new() { TenantId = "tenant-b" });
            await Assert.That(tenantBRoles).HasSingleItem();
            await Assert.That(tenantBRoles.Single().Id).IsEqualTo(roleB.Role.Id);

            tenantBRoles.Single().Name = "Operators B";
            await _roleStore.SaveAsync(tenantBRoles.Single());

            await Assert.That((await _roleStore.FindAsync(new() { Id = roleB.Role.Id }))!.Name).IsEqualTo("Operators B");
        }

        var tenantARole = await _roleStore.FindAsync(new() { Id = roleA.Role.Id });
        await Assert.That(tenantARole).IsNotNull();
        await Assert.That(tenantARole.Name).IsEqualTo("Operators");
        await Assert.That(tenantARole.Permissions).IsEquivalentTo(["tenant-a:permission"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        tenantARole.Name = "Operators A";
        await _roleStore.SaveAsync(tenantARole);
        await Assert.That((await _roleStore.FindAsync(new() { Id = roleA.Role.Id }))!.Name).IsEqualTo("Operators A");

        await _roleStore.DeleteAsync(new() { Id = roleA.Role.Id });
        await Assert.That(await _roleStore.FindManyAsync(new() { TenantId = "tenant-a" })).IsEmpty();

        using (_tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            var remainingTenantBRole = await _roleStore.FindAsync(new() { Id = roleA.Role.Id });
            await Assert.That(remainingTenantBRole).IsNotNull();
            await Assert.That(remainingTenantBRole.Name).IsEqualTo("Operators B");
        }
    }

    [Test]
    public async Task DefaultTenantListsLegacyRolesWithoutATenantId()
    {
        var tenantAccessor = new TestTenantAccessor();
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), tenantAccessor);

        await roleStore.SaveAsync(new Role { Id = "legacy", Name = "Legacy", Permissions = [] });

        var roles = await roleStore.FindManyAsync(new() { TenantId = Tenant.DefaultTenantId });

        await Assert.That(roles).HasSingleItem();
        await Assert.That(roles.Single().Id).IsEqualTo("legacy");
    }

    [Test]
    public async Task CreateRoleRejectsExistingRoleId()
    {
        await _roleStore.SaveAsync(new Role
        {
            Id = "admin",
            Name = "Admin",
            TenantId = "tenant-a",
            Permissions = [PermissionNames.All]
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _manager.CreateRoleAsync("Replacement", [], "admin"));

        var role = await _roleStore.FindAsync(new() { Id = "admin" });
        await Assert.That(role).IsNotNull();
        await Assert.That(role.Name).IsEqualTo("Admin");
        await Assert.That(role.Permissions).IsEquivalentTo([PermissionNames.All], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task CreateRoleRejectsProvidedAdminRoleIdCollision()
    {
        var manager = new RoleManager(_roleStore, new AdminRoleProvider(), _tenantAccessor);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.CreateRoleAsync("Replacement", [], "admin"));
    }
}