using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Identity.UnitTests.HostedServices;

public class AdminUserInitializerTests
{
    [Fact]
    public async Task ExecuteAsyncAddsMissingConfiguredPermissionsToExistingAdminRole()
    {
        var roleStore = await CreateRoleStoreAsync(["custom"]);
        var initializer = CreateInitializer(roleStore, ["*"]);

        await initializer.ExecuteAsync(CancellationToken.None);

        var role = await roleStore.FindAsync(new RoleFilter { Id = "admin" });
        Assert.NotNull(role);
        Assert.Equal(["custom", "*"], role.Permissions);
    }

    [Fact]
    public async Task ExecuteAsyncDoesNotDuplicateExistingConfiguredPermissions()
    {
        var roleStore = await CreateRoleStoreAsync(["*", "custom"]);
        var initializer = CreateInitializer(roleStore, ["*"]);

        await initializer.ExecuteAsync(CancellationToken.None);
        await initializer.ExecuteAsync(CancellationToken.None);

        var role = await roleStore.FindAsync(new RoleFilter { Id = "admin" });
        Assert.NotNull(role);
        Assert.Equal(["*", "custom"], role.Permissions);
    }

    private static async Task<MemoryRoleStore> CreateRoleStoreAsync(ICollection<string> permissions)
    {
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>());
        await roleStore.AddAsync(new Role
        {
            Id = "admin",
            Name = "Administrator",
            Permissions = permissions
        });
        return roleStore;
    }

    private static AdminUserInitializer CreateInitializer(IRoleStore roleStore, ICollection<string> permissions)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DefaultAdminUserOptions
        {
            AdminRoleName = "admin",
            AdminRolePermissions = permissions
        });
        return new(
            Substitute.For<IUserStore>(),
            roleStore,
            Substitute.For<IUserManager>(),
            Substitute.For<IRoleManager>(),
            options,
            NullLogger<AdminUserInitializer>.Instance);
    }
}
