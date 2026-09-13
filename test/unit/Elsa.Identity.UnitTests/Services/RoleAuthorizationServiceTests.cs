using Elsa.Testing.Shared.Multitenancy;
using Elsa.Authorization;
using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class RoleAuthorizationServiceTests
{
    private readonly MemoryRoleStore _roleStore;
    private readonly RoleAuthorizationService _service;

    public RoleAuthorizationServiceTests()
    {
        _roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        _service = new RoleAuthorizationService(new StoreBasedRoleProvider(_roleStore), new PermissionEvaluator());
    }

    [Test]
    public async Task CannotAssignAdminRoleWithOnlyUserWritePermission()
    {
        await AddRoleAsync("admin", PermissionNames.All);

        var canAssignRoles = await _service.CanAssignRolesAsync(CreateUser("create:user"), ["admin"]);

        await Assert.That(canAssignRoles).IsFalse();
    }

    [Test]
    public async Task CanAssignAdminRoleWithAllPermission()
    {
        await AddRoleAsync("admin", PermissionNames.All);

        var canAssignRoles = await _service.CanAssignRolesAsync(CreateUser(PermissionNames.All), ["admin"]);

        await Assert.That(canAssignRoles).IsTrue();
    }

    [Test]
    public async Task CannotAssignUnknownRoleId()
    {
        var canAssignRoles = await _service.CanAssignRolesAsync(CreateUser(PermissionNames.All), ["future-admin"]);

        await Assert.That(canAssignRoles).IsFalse();
    }

    [Test]
    public async Task CannotMutatePrivilegedRoleWithOnlyRoleUpdatePermission()
    {
        var role = new Role
        {
            Id = "admin",
            Name = "Admin",
            Permissions = [PermissionNames.All]
        };

        var canMutateRole = _service.CanMutateRole(CreateUser("update:role"), role, ["read:workflow-definitions"]);

        await Assert.That(canMutateRole).IsFalse();
    }

    private Task AddRoleAsync(string id, params string[] permissions)
    {
        return _roleStore.SaveAsync(new Role
        {
            Id = id,
            Name = id,
            Permissions = permissions
        });
    }

    private static ClaimsPrincipal CreateUser(params string[] permissions)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaims(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)));
        return new(identity);
    }
}