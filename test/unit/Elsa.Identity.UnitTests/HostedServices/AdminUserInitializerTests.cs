using Elsa.Testing.Shared.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.HostedServices;

public class AdminUserInitializerTests
{
    [Test]
    public async Task ExecuteAsyncAddsMissingConfiguredPermissionsToExistingAdminRole()
    {
        var roleStore = await CreateRoleStoreAsync(["custom"]);
        var initializer = CreateInitializer(roleStore, ["*"]);

        await initializer.ExecuteAsync(CancellationToken.None);

        var role = await roleStore.FindAsync(new RoleFilter { Id = "admin" });
        await Assert.That(role).IsNotNull();
        await Assert.That(role.Permissions).IsEquivalentTo(["custom", "*"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExecuteAsyncDoesNotDuplicateExistingConfiguredPermissions()
    {
        var roleStore = await CreateRoleStoreAsync(["*", "custom"]);
        var initializer = CreateInitializer(roleStore, ["*"]);

        await initializer.ExecuteAsync(CancellationToken.None);
        await initializer.ExecuteAsync(CancellationToken.None);

        var role = await roleStore.FindAsync(new RoleFilter { Id = "admin" });
        await Assert.That(role).IsNotNull();
        await Assert.That(role.Permissions).IsEquivalentTo(["*", "custom"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    private static async Task<MemoryRoleStore> CreateRoleStoreAsync(ICollection<string> permissions)
    {
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
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