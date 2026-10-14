using System.Security.Claims;
using Elsa.Authorization;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// The target-user check must see the same permissions the target's tokens carry, whatever the role provider does with IDs.
/// </summary>
public class UserManagementAuthorizationTests
{
    private static readonly ClaimsPrincipal Manager = CreateCaller("identity/users:update", "identity/users:delete");
    private static readonly ClaimsPrincipal FullAdmin = CreateCaller(PermissionNames.All);

    [Fact]
    public async Task CaseVariantRoleIdResolvedByACaseInsensitiveProviderIsStillChecked()
    {
        // Simulates a case-insensitive collation: "ADMIN" resolves to the role stored as "admin".
        var provider = Substitute.For<IRoleProvider>();
        provider.FindManyAsync(Arg.Is<RoleFilter>(x => x.Ids!.Any(id => string.Equals(id, "admin", StringComparison.OrdinalIgnoreCase))), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IEnumerable<Role>>([new Role { Id = "admin", Name = "admin", Permissions = [PermissionNames.All] }]));
        var target = CreateTarget("ADMIN");

        Assert.False(await CanManageAsync(provider, Manager, target));
        Assert.True(await CanManageAsync(provider, FullAdmin, target));
    }

    [Fact]
    public async Task AdminRoleProviderTreatsEveryTargetAsAdmin()
    {
        var provider = new AdminRoleProvider();
        var target = CreateTarget("editor");

        Assert.False(await CanManageAsync(provider, Manager, target));
        Assert.True(await CanManageAsync(provider, FullAdmin, target));
    }

    [Fact]
    public async Task DeletedRoleIdsDoNotBlockManagement()
    {
        var provider = await CreateStoreProviderAsync(new Role { Id = "admin", Name = "admin", Permissions = [PermissionNames.All] });

        Assert.True(await CanManageAsync(provider, Manager, CreateTarget("deleted-role")));
        Assert.True(await CanManageAsync(provider, FullAdmin, CreateTarget("deleted-role")));
    }

    [Fact]
    public async Task DeletedRoleIdsDoNotHideTheRemainingRoles()
    {
        var provider = await CreateStoreProviderAsync(new Role { Id = "admin", Name = "admin", Permissions = [PermissionNames.All] });
        var target = CreateTarget("deleted-role", "admin");

        Assert.False(await CanManageAsync(provider, Manager, target));
        Assert.True(await CanManageAsync(provider, FullAdmin, target));
    }

    private static Task<bool> CanManageAsync(IRoleProvider provider, ClaimsPrincipal caller, User target) =>
        new RoleAuthorizationService(provider, new PermissionEvaluator()).CanManageUserAsync(provider, caller, target, CancellationToken.None);

    private static async Task<IRoleProvider> CreateStoreProviderAsync(params Role[] roles)
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        foreach (var role in roles)
            await store.SaveAsync(role);
        return new StoreBasedRoleProvider(store);
    }

    private static User CreateTarget(params string[] roles) => new() { Id = "target", Name = "target", Roles = roles.ToList() };

    private static ClaimsPrincipal CreateCaller(params string[] permissions)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaims(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)));
        return new(identity);
    }
}
