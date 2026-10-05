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
using Elsa.Common.Multitenancy;

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

    [Fact]
    public async Task ExecuteAsyncCreatesTheAdminRoleWithAGeneratedIdAndAssignsThatIdToTheUser()
    {
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        var roleManager = Substitute.For<IRoleManager>();
        roleManager.CreateRoleAsync("admin", Arg.Any<ICollection<string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CreateRoleResult(new Role { Id = "generated-role", Name = "admin", Permissions = ["*"] }));
        var userManager = CreateUserManager();

        await CreateInitializer(roleStore, ["*"], roleManager, userManager, withUser: true).ExecuteAsync(CancellationToken.None);

        // No ID is passed, so the role manager generates one rather than reusing AdminRoleName.
        await roleManager.Received(1).CreateRoleAsync("admin", Arg.Any<ICollection<string>?>(), null, Arg.Any<CancellationToken>());
        await AssertAdminUserCreatedWithRoleAsync(userManager, "generated-role");
    }

    [Fact]
    public async Task ExecuteAsyncFindsAnAdminRoleWithAGeneratedIdByName()
    {
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await roleStore.SaveAsync(new Role { Id = "generated-role", Name = "admin", Permissions = ["*"] });
        var roleManager = Substitute.For<IRoleManager>();
        var userManager = CreateUserManager();

        await CreateInitializer(roleStore, ["*"], roleManager, userManager, withUser: true).ExecuteAsync(CancellationToken.None);

        await roleManager.DidNotReceiveWithAnyArgs().CreateRoleAsync(default!);
        await AssertAdminUserCreatedWithRoleAsync(userManager, "generated-role");
    }

    [Fact]
    public async Task ExecuteAsyncDoesNotReuseAnotherTenantsAdminRoleFromASharedStore()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-b");
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), tenantAccessor);
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a", Name = "Tenant A" }))
            await roleStore.SaveAsync(new Role { Id = "admin", Name = "admin", TenantId = "tenant-a", Permissions = ["*"] });
        var roleManager = Substitute.For<IRoleManager>();
        roleManager.CreateRoleAsync("admin", Arg.Any<ICollection<string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CreateRoleResult(new Role { Id = "generated-role", Name = "admin", TenantId = "tenant-b", Permissions = ["*"] }));
        var userManager = CreateUserManager();

        await CreateInitializer(roleStore, ["*"], roleManager, userManager, withUser: true).ExecuteAsync(CancellationToken.None);

        await roleManager.Received(1).CreateRoleAsync("admin", Arg.Any<ICollection<string>?>(), null, Arg.Any<CancellationToken>());
        await AssertAdminUserCreatedWithRoleAsync(userManager, "generated-role");
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a", Name = "Tenant A" }))
            Assert.Equal(["*"], (await roleStore.FindAsync(new RoleFilter { Id = "admin" }))!.Permissions);
    }

    [Fact]
    public async Task ExecuteAsyncReusesAnAdminRoleWhoseNameDiffersOnlyInCase()
    {
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await roleStore.SaveAsync(new Role { Id = "generated-role", Name = "Admin", Permissions = ["*"] });
        var roleManager = Substitute.For<IRoleManager>();
        var userManager = CreateUserManager();

        await CreateInitializer(roleStore, ["*"], roleManager, userManager, withUser: true).ExecuteAsync(CancellationToken.None);

        await roleManager.DidNotReceiveWithAnyArgs().CreateRoleAsync(default!);
        await AssertAdminUserCreatedWithRoleAsync(userManager, "generated-role");
    }

    private static Task AssertAdminUserCreatedWithRoleAsync(IUserManager userManager, string roleId) =>
        userManager.Received(1).CreateUserAsync(
            "admin",
            "password",
            Arg.Is<ICollection<string>?>(x => x != null && x.SequenceEqual(new[] { roleId })),
            Arg.Any<CancellationToken>());

    private static IUserManager CreateUserManager()
    {
        var userManager = Substitute.For<IUserManager>();
        userManager.CreateUserAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<ICollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(call => new CreateUserResult(new User { Id = "user-1", Name = call.ArgAt<string>(0), Roles = call.ArgAt<ICollection<string>?>(2)!.ToList() }, "password", false));
        return userManager;
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

    private static AdminUserInitializer CreateInitializer(IRoleStore roleStore, ICollection<string> permissions) =>
        CreateInitializer(roleStore, permissions, Substitute.For<IRoleManager>(), Substitute.For<IUserManager>(), withUser: false);

    private static AdminUserInitializer CreateInitializer(IRoleStore roleStore, ICollection<string> permissions, IRoleManager roleManager, IUserManager userManager, bool withUser)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DefaultAdminUserOptions
        {
            AdminUserName = withUser ? "admin" : "",
            AdminPassword = withUser ? "password" : "",
            AdminRoleName = "admin",
            AdminRolePermissions = permissions
        });
        var userStore = Substitute.For<IUserStore>();
        userStore.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        return new(
            userStore,
            roleStore,
            userManager,
            roleManager,
            options,
            NullLogger<AdminUserInitializer>.Instance);
    }
}
