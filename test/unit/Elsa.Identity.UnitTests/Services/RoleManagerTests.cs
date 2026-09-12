using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;

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

    [Fact]
    public async Task CreateListUpdateAndDeleteAreIsolatedForRolesWithTheSameNameAcrossTenants()
    {
        var roleA = await _manager.CreateRoleAsync("Operators", ["tenant-a:permission"]);

        Assert.Equal("tenant-a", roleA.Role.TenantId);
        Assert.Single(await _roleStore.FindManyAsync(new() { TenantId = "tenant-a" }));

        using (_tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            var roleB = await _manager.CreateRoleAsync("Operators", ["tenant-b:permission"]);

            Assert.Equal(roleA.Role.Id, roleB.Role.Id);
            Assert.Equal("tenant-b", roleB.Role.TenantId);
            Assert.Equal(["tenant-b:permission"], roleB.Role.Permissions);

            var tenantBRoles = await _roleStore.FindManyAsync(new() { TenantId = "tenant-b" });
            Assert.Single(tenantBRoles);
            Assert.Equal(roleB.Role.Id, tenantBRoles.Single().Id);

            tenantBRoles.Single().Name = "Operators B";
            await _roleStore.SaveAsync(tenantBRoles.Single());

            Assert.Equal("Operators B", (await _roleStore.FindAsync(new() { Id = roleB.Role.Id }))!.Name);
        }

        var tenantARole = await _roleStore.FindAsync(new() { Id = roleA.Role.Id });
        Assert.NotNull(tenantARole);
        Assert.Equal("Operators", tenantARole.Name);
        Assert.Equal(["tenant-a:permission"], tenantARole.Permissions);

        tenantARole.Name = "Operators A";
        await _roleStore.SaveAsync(tenantARole);
        Assert.Equal("Operators A", (await _roleStore.FindAsync(new() { Id = roleA.Role.Id }))!.Name);

        await _roleStore.DeleteAsync(new() { Id = roleA.Role.Id });
        Assert.Empty(await _roleStore.FindManyAsync(new() { TenantId = "tenant-a" }));

        using (_tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            var remainingTenantBRole = await _roleStore.FindAsync(new() { Id = roleA.Role.Id });
            Assert.NotNull(remainingTenantBRole);
            Assert.Equal("Operators B", remainingTenantBRole.Name);
        }
    }

    [Fact]
    public async Task DefaultTenantListsLegacyRolesWithoutATenantId()
    {
        var tenantAccessor = new TestTenantAccessor();
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), tenantAccessor);

        await roleStore.SaveAsync(new Role { Id = "legacy", Name = "Legacy", Permissions = [] });

        var roles = await roleStore.FindManyAsync(new() { TenantId = Tenant.DefaultTenantId });

        Assert.Single(roles);
        Assert.Equal("legacy", roles.Single().Id);
    }

    [Fact]
    public async Task CreateRoleRejectsExistingRoleId()
    {
        await _roleStore.SaveAsync(new Role
        {
            Id = "admin",
            Name = "Admin",
            TenantId = "tenant-a",
            Permissions = [PermissionNames.All]
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.CreateRoleAsync("Replacement", [], "admin"));

        var role = await _roleStore.FindAsync(new() { Id = "admin" });
        Assert.NotNull(role);
        Assert.Equal("Admin", role.Name);
        Assert.Equal([PermissionNames.All], role.Permissions);
    }

    [Fact]
    public async Task CreateRoleRejectsProvidedAdminRoleIdCollision()
    {
        var manager = new RoleManager(_roleStore, new AdminRoleProvider(), _tenantAccessor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateRoleAsync("Replacement", [], "admin"));
    }
}
