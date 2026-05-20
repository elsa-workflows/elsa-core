using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class RoleManagerTests
{
    private readonly MemoryRoleStore _roleStore;
    private readonly RoleManager _manager;

    public RoleManagerTests()
    {
        _roleStore = new MemoryRoleStore(new MemoryStore<Role>());
        _manager = new RoleManager(_roleStore, new StoreBasedRoleProvider(_roleStore));
    }

    [Fact]
    public async Task CreateRoleRejectsExistingRoleId()
    {
        await _roleStore.SaveAsync(new Role
        {
            Id = "admin",
            Name = "Admin",
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
        var manager = new RoleManager(_roleStore, new AdminRoleProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateRoleAsync("Replacement", [], "admin"));
    }
}
