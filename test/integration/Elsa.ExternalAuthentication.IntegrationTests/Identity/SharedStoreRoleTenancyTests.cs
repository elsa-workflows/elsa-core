using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

/// <summary>
/// Tenants that share one identity store each hold their own roles, including same-named ones, while roles and
/// role references created before role IDs were generated keep working (#8615). Held by the in-memory store and by
/// the migrated EF Core store with multitenancy on.
/// </summary>
public abstract class SharedStoreRoleTenancyTests : IAsyncLifetime
{
    private const string AdminUserName = "admin";
    private const string AdminRoleName = "admin";
    private static readonly Tenant TenantA = new() { Id = "tenant-a", Name = "Tenant A" };
    private static readonly Tenant TenantB = new() { Id = "tenant-b", Name = "Tenant B" };

    protected TestTenantAccessor TenantAccessor { get; } = new();

    protected IRoleStore RoleStore { get; private set; } = null!;

    protected IUserStore UserStore { get; private set; } = null!;

    private IRoleProvider RoleProvider => new StoreBasedRoleProvider(RoleStore);

    private RoleManager RoleManager => new(RoleStore, RoleProvider, TenantAccessor, new GuidIdentityGenerator());

    protected abstract Task<(IRoleStore RoleStore, IUserStore UserStore)> CreateStoresAsync();

    protected virtual Task DisposeStoresAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => (RoleStore, UserStore) = await CreateStoresAsync();

    public Task DisposeAsync() => DisposeStoresAsync();

    [Fact]
    public async Task EachTenantSharingTheStoreGetsItsOwnSeededAdmin()
    {
        var adminA = await SeedAdminAsync(TenantA);
        var adminB = await SeedAdminAsync(TenantB);

        Assert.NotEqual(adminA.Role.Id, adminB.Role.Id);
        Assert.Equal(TenantA.Id, adminA.Role.TenantId);
        Assert.Equal(TenantB.Id, adminB.Role.TenantId);
        Assert.Equal([adminA.Role.Id], adminA.User.Roles);
        Assert.Equal([adminB.Role.Id], adminB.User.Roles);
        Assert.NotEqual(adminA.User.Id, adminB.User.Id);

        // The user's role reference resolves to the role that grants the configured permissions.
        Assert.Equal([PermissionNames.All], await ResolvePermissionsAsync(TenantA, adminA.User));
        Assert.Equal([PermissionNames.All], await ResolvePermissionsAsync(TenantB, adminB.User));
    }

    [Fact]
    public async Task SeedingAgainReusesTheTenantsAdminRole()
    {
        var first = await SeedAdminAsync(TenantA);
        await SeedAdminAsync(TenantB);
        var second = await SeedAdminAsync(TenantA);

        Assert.Equal(first.Role.Id, second.Role.Id);
        Assert.Equal(first.User.Id, second.User.Id);
        using (TenantAccessor.PushContext(TenantA))
            Assert.Single(await RoleStore.FindManyAsync(new()));
    }

    [Fact]
    public async Task BothTenantsCanCreateASameNamedRole()
    {
        string roleAId;
        string roleBId;

        using (TenantAccessor.PushContext(TenantA))
            roleAId = (await RoleManager.CreateRoleAsync("Operators", ["tenant-a:permission"])).Role.Id;

        using (TenantAccessor.PushContext(TenantB))
            roleBId = (await RoleManager.CreateRoleAsync("Operators", ["tenant-b:permission"])).Role.Id;

        Assert.NotEqual(roleAId, roleBId);

        using (TenantAccessor.PushContext(TenantA))
        {
            var role = await RoleStore.FindByNameAsync("Operators");
            Assert.Equal(roleAId, role?.Id);
            Assert.Equal(["tenant-a:permission"], role?.Permissions);
            await Assert.ThrowsAsync<InvalidOperationException>(() => RoleManager.CreateRoleAsync("Operators"));
        }

        using (TenantAccessor.PushContext(TenantB))
        {
            var role = await RoleStore.FindByNameAsync("Operators");
            Assert.Equal(roleBId, role?.Id);
            Assert.Equal(["tenant-b:permission"], role?.Permissions);
        }
    }

    [Fact]
    public async Task ARoleInOneTenantIsInvisibleToAnother()
    {
        string roleAId;
        using (TenantAccessor.PushContext(TenantA))
            roleAId = (await RoleManager.CreateRoleAsync("Auditors", ["tenant-a:permission"])).Role.Id;

        using (TenantAccessor.PushContext(TenantB))
        {
            Assert.Null(await RoleStore.FindAsync(new() { Id = roleAId }));
            Assert.Null(await RoleStore.FindByNameAsync("Auditors"));
            Assert.Empty(await RoleStore.FindManyAsync(new()));
            Assert.Empty(await RoleProvider.FindByIdsAsync([roleAId]));

            // A tenant-B user that references tenant A's role ID gains nothing from it.
            Assert.Empty(await ResolvePermissionsAsync(TenantB, new User { Id = "intruder", Name = "intruder", Roles = [roleAId] }));
        }
    }

    [Fact]
    public async Task ALegacyNameDerivedRoleAndTheUserReferencingItKeepWorking()
    {
        // What earlier versions stored in a single-tenant deployment: the role ID is the role name and the
        // seeded user references that ID. Rows written before tenant assignment carry no tenant (the EF saving
        // handler stamps the default tenant instead), and both are visible to the default tenant.
        await RoleStore.SaveAsync(new Role { Id = AdminRoleName, Name = AdminRoleName, TenantId = null, Permissions = [PermissionNames.All] });
        await RoleStore.SaveAsync(new Role { Id = "power-user", Name = "Power User", TenantId = null, Permissions = ["workflows/*:view"] });
        await UserStore.SaveAsync(new User { Id = "legacy-admin", Name = AdminUserName, TenantId = null, Roles = [AdminRoleName, "power-user"] });

        var seeded = await SeedAdminAsync(null);

        Assert.Equal(AdminRoleName, seeded.Role.Id);
        Assert.Equal("legacy-admin", seeded.User.Id);
        Assert.Equal([AdminRoleName, "power-user"], seeded.User.Roles);
        Assert.Equal([PermissionNames.All, "workflows/*:view"], await ResolvePermissionsAsync(null, seeded.User));
        Assert.Equal(2, (await RoleStore.FindManyAsync(new())).Count());

        // The legacy role still owns its name, so a same-named role is still rejected in its tenant.
        await Assert.ThrowsAsync<InvalidOperationException>(() => RoleManager.CreateRoleAsync("Power User"));
    }

    [Fact]
    public async Task ATenantActivatedAfterAnUpgradeGetsItsOwnAdminAndLeavesTheLegacyOneAlone()
    {
        // Before the fix, the first tenant activated took the store-wide "admin" role ID; later tenants got none.
        await SaveAsAsync(TenantA, new Role { Id = AdminRoleName, Name = AdminRoleName, TenantId = TenantA.Id, Permissions = [PermissionNames.All] });
        await SaveAsAsync(TenantA, new User { Id = "admin-a", Name = AdminUserName, TenantId = TenantA.Id, Roles = [AdminRoleName] });

        var adminA = await SeedAdminAsync(TenantA);
        var adminB = await SeedAdminAsync(TenantB);

        Assert.Equal(AdminRoleName, adminA.Role.Id);
        Assert.Equal("admin-a", adminA.User.Id);
        Assert.NotEqual(AdminRoleName, adminB.Role.Id);
        Assert.Equal(TenantB.Id, adminB.Role.TenantId);
        Assert.Equal([adminB.Role.Id], adminB.User.Roles);
        Assert.Equal([PermissionNames.All], await ResolvePermissionsAsync(TenantA, adminA.User));
        Assert.Equal([PermissionNames.All], await ResolvePermissionsAsync(TenantB, adminB.User));

        using (TenantAccessor.PushContext(TenantA))
        {
            var legacyRole = await RoleStore.FindAsync(new() { Id = AdminRoleName });
            Assert.Equal(TenantA.Id, legacyRole?.TenantId);
        }
    }

    [Fact]
    public async Task SeedingNeitherReusesNorWidensATenantAgnosticAdminRole()
    {
        // A platform role shared by every tenant, which happens to carry the admin role's ID and name.
        await RoleStore.SaveAsync(new Role { Id = AdminRoleName, Name = AdminRoleName, TenantId = Tenant.AgnosticTenantId, Permissions = ["workflows/*:view"] });

        var adminB = await SeedAdminAsync(TenantB);

        Assert.NotEqual(AdminRoleName, adminB.Role.Id);
        Assert.Equal(TenantB.Id, adminB.Role.TenantId);
        Assert.Equal([adminB.Role.Id], adminB.User.Roles);
        using (TenantAccessor.PushContext(TenantA))
        {
            var sharedRole = await RoleStore.FindAsync(new() { Id = AdminRoleName });
            Assert.Equal(Tenant.AgnosticTenantId, sharedRole?.TenantId);
            Assert.Equal(["workflows/*:view"], sharedRole?.Permissions);
        }
    }

    private async Task<(Role Role, User User)> SeedAdminAsync(Tenant? tenant)
    {
        using (TenantAccessor.PushContext(tenant))
        {
            var userManager = new UserManager(new GuidIdentityGenerator(), new DefaultSecretGenerator(new DefaultRandomStringGenerator()), new DefaultSecretHasher(), UserStore, TenantAccessor);
            var options = Microsoft.Extensions.Options.Options.Create(new DefaultAdminUserOptions
            {
                AdminUserName = AdminUserName,
                AdminPassword = "bootstrap-password",
                AdminRoleName = AdminRoleName,
                AdminRolePermissions = [PermissionNames.All]
            });
            var initializer = new AdminUserInitializer(UserStore, RoleStore, userManager, RoleManager, options, NullLogger<AdminUserInitializer>.Instance);

            await initializer.ExecuteAsync(CancellationToken.None);

            var user = await UserStore.FindAsync(new() { Name = AdminUserName });
            Assert.NotNull(user);
            var role = Assert.Single(await RoleProvider.FindByIdsAsync(user.Roles.Where(x => x != "power-user")));
            return (role, user);
        }
    }

    private async Task<IReadOnlyCollection<string>> ResolvePermissionsAsync(Tenant? tenant, User user)
    {
        using (TenantAccessor.PushContext(tenant))
            return (await RoleProvider.FindByIdsAsync(user.Roles)).SelectMany(x => x.Permissions).Order(StringComparer.Ordinal).ToList();
    }

    private async Task SaveAsAsync(Tenant tenant, Role role)
    {
        using (TenantAccessor.PushContext(tenant))
            await RoleStore.SaveAsync(role);
    }

    private async Task SaveAsAsync(Tenant tenant, User user)
    {
        using (TenantAccessor.PushContext(tenant))
            await UserStore.SaveAsync(user);
    }
}

public sealed class MemorySharedStoreRoleTenancyTests : SharedStoreRoleTenancyTests
{
    protected override Task<(IRoleStore RoleStore, IUserStore UserStore)> CreateStoresAsync() =>
        Task.FromResult<(IRoleStore, IUserStore)>((
            new MemoryRoleStore(new MemoryStore<Role>(), TenantAccessor),
            new MemoryUserStore(new MemoryStore<User>(), TenantAccessor)));
}

public sealed class SqliteSharedStoreRoleTenancyTests : SharedStoreRoleTenancyTests
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-identity-shared-roles-{Guid.NewGuid():N}.db");
    private ServiceProvider? _services;
    private IServiceScope? _scope;

    protected override async Task<(IRoleStore RoleStore, IUserStore UserStore)> CreateStoresAsync()
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(TenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<IdentityElsaDbContext>((_, builder) =>
                builder.UseElsaSqlite(typeof(IdentityDbContextFactory).Assembly, $"Data Source={_databasePath};Default Timeout=30"))
            .Decorate<IDbContextFactory<IdentityElsaDbContext>, TenantAwareDbContextFactory<IdentityElsaDbContext>>()
            .AddScoped<EntityStore<IdentityElsaDbContext, Role>>()
            .AddScoped<EntityStore<IdentityElsaDbContext, User>>()
            .AddScoped<EFCoreRoleStore>()
            .AddScoped<EFCoreUserStore>()
            .BuildServiceProvider();

        // Migrated rather than created, so these assertions run against the shipped Roles and Users schema.
        await using (var dbContext = await _services.GetRequiredService<IDbContextFactory<IdentityElsaDbContext>>().CreateDbContextAsync())
            await dbContext.Database.MigrateAsync();

        _scope = _services.CreateScope();
        return (_scope.ServiceProvider.GetRequiredService<EFCoreRoleStore>(), _scope.ServiceProvider.GetRequiredService<EFCoreUserStore>());
    }

    protected override async Task DisposeStoresAsync()
    {
        _scope?.Dispose();

        if (_services is not null)
            await _services.DisposeAsync();

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
